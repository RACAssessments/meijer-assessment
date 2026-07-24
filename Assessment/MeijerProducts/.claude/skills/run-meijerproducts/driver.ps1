<#
.SYNOPSIS
  Build, launch, drive, and screenshot the MeijerProducts MAUI Windows app.

.DESCRIPTION
  Commands:
    launch      Build (unless -NoBuild) and start the app; waits for the window, saves its
                PID/HWND to a state file so later commands can find it.
    screenshot  -Out <path>   Full virtual-screen PNG screenshot.
    click       [-Name <text>]  Invoke a button via UI Automation. If -Name is omitted, clicks
                the first button found (this app currently has exactly one).
    get-text    [-Name <text>]  Print a button's current accessible Name/text.
    stop        Kill the tracked process and clear the state file.

.EXAMPLE
  powershell -File driver.ps1 launch
  powershell -File driver.ps1 screenshot -Out C:\tmp\before.png
  powershell -File driver.ps1 click -Name "Click me"
  powershell -File driver.ps1 screenshot -Out C:\tmp\after.png
  powershell -File driver.ps1 stop
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('launch', 'screenshot', 'click', 'get-text', 'stop')]
    [string]$Command,

    [string]$Name,
    [string]$Out,
    [string]$Configuration = 'Debug',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

# Skill dir -> up 3 (.claude/skills/run-meijerproducts -> .claude/skills -> .claude -> project root)
$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$Csproj = Join-Path $ProjectDir 'MeijerProducts.csproj'
$TargetFramework = 'net10.0-windows10.0.19041.0'
$StateFile = Join-Path $env:TEMP 'meijerproducts-driver-state.json'

function Get-ExePath {
    Join-Path $ProjectDir "bin\$Configuration\$TargetFramework\win-x64\MeijerProducts.exe"
}

function Save-DriverState([int]$ProcId, [int64]$Hwnd) {
    @{ ProcessId = $ProcId; Hwnd = $Hwnd } | ConvertTo-Json | Set-Content -Path $StateFile
}

function Get-DriverState {
    if (-not (Test-Path $StateFile)) {
        throw "No running app tracked ($StateFile missing). Run the 'launch' command first."
    }
    Get-Content $StateFile -Raw | ConvertFrom-Json
}

function Get-AutomationRoot([int64]$Hwnd) {
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $h = [IntPtr]$Hwnd
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($h)
    if ($null -eq $root) {
        throw "Could not attach to window handle $Hwnd - is the app still running? Try 'launch' again."
    }
    return $root
}

# Window-chrome caption buttons (Minimize/Maximize/Close/Restore/System Menu) show up as
# ControlType.Button too and are the *first* Descendants match in document order - always
# exclude them by name, even when a specific -ButtonName was requested, so a typo'd name
# can't accidentally "succeed" by hitting a caption button.
$script:ChromeButtonNames = @('Minimize', 'Maximize', 'Close', 'Restore', 'System Menu')

function Find-AppButton($Root, [string]$ButtonName) {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    $all = $Root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    $candidates = @($all | Where-Object { $script:ChromeButtonNames -notcontains $_.Current.Name })

    if ($ButtonName) {
        $match = $candidates | Where-Object { $_.Current.Name -eq $ButtonName } | Select-Object -First 1
        if ($null -eq $match) {
            throw "No app button named '$ButtonName' found (saw: $(($candidates | ForEach-Object { $_.Current.Name }) -join ', '))."
        }
        return $match
    }

    if ($candidates.Count -eq 0) {
        throw "No app buttons found in the window (only window-chrome buttons present)."
    }
    return $candidates[0]
}

switch ($Command) {
    'launch' {
        if (-not $NoBuild) {
            Write-Host "Building ($Configuration, $TargetFramework)..."
            & dotnet build $Csproj -c $Configuration -f $TargetFramework | Out-Host
            if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }
        }
        $exe = Get-ExePath
        if (-not (Test-Path $exe)) {
            throw "Executable not found at $exe - build first (omit -NoBuild)."
        }
        $p = Start-Process -FilePath $exe -PassThru
        $hwnd = 0
        for ($i = 0; $i -lt 30; $i++) {
            Start-Sleep -Milliseconds 500
            $p.Refresh()
            if ($p.MainWindowHandle -ne 0) { $hwnd = $p.MainWindowHandle; break }
        }
        if ($hwnd -eq 0) {
            throw "App started (PID $($p.Id)) but no window appeared within 15s."
        }
        Save-DriverState -ProcId $p.Id -Hwnd $hwnd
        Write-Host "Launched. PID=$($p.Id) HWND=$hwnd"
    }
    'screenshot' {
        if (-not $Out) { throw "-Out <path> is required." }
        $outDir = Split-Path -Parent $Out
        if ($outDir -and -not (Test-Path $outDir)) {
            New-Item -ItemType Directory -Path $outDir -Force | Out-Null
        }
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
        $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
        $bmp = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
        $g.Dispose()
        $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "Saved screenshot: $Out"
    }
    'click' {
        $state = Get-DriverState
        $root = Get-AutomationRoot $state.Hwnd
        $btn = Find-AppButton $root $Name
        $invoke = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Start-Sleep -Milliseconds 300
        Write-Host "Clicked. Current text: $($btn.Current.Name)"
    }
    'get-text' {
        $state = Get-DriverState
        $root = Get-AutomationRoot $state.Hwnd
        $btn = Find-AppButton $root $Name
        Write-Output $btn.Current.Name
    }
    'stop' {
        $state = Get-DriverState
        Stop-Process -Id $state.ProcessId -Force -ErrorAction SilentlyContinue
        Remove-Item $StateFile -ErrorAction SilentlyContinue
        Write-Host "Stopped PID $($state.ProcessId)."
    }
}
