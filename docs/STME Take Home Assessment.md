## Meijer Interview Coding Challenge:

| Title | Product Information API & UI Implementation in MAUI mobile application. |
| --- | --- |
| Task 1: | Develop & Implement two Product API calls (Product API, Product Detail API) to retrieve, prepare data and display. The sample responses are in the next page |
| Task 2 (UI/UX ): | Develop a MAUI App with One screen for Listing products summary with image icon, title, summary and Detail screen with full image, title, description, price and include a add to list functionality in Detail screen to share that you are interested in the product, example screen mockups provided below. o Add to list: On clicking add to list button/icon the app makes the following information of the format “{product title} - {price} from {city name from current user location} added to list "shareable through any of the available options like text message apps, social media apps and/or email. Example string: “Bananas - $0.59 / lb. from Chicago added to list” |
| Success Criteria | 1. Backend: .NET Working APIs retrieving data, use persistence layer of your choice but implement the APIs with best practices. 2. MAUI: First and foremost: Working code using MAUI with Task 1 & 2 completed and with an ability to speak to what you did. 3. Organize and structure the code with architecture, best standards, and practices. Unit tests is a plus. 4. Feel free to add anything additional that you would like. |

*Please do share all your code artifacts along with any AI tools or help that were used to complete the assessments (Prompts, Instructions, Skills, Context files).


## API Samples for Reference (JSON Objects also attached) :

## Products API (GET):

```
\- Sample Response:
[
{
"id": 0,
"imageUrl":
"https://www.meijer.com/content/dam/meijer/product/0000/00/4011/00/0000004011000_1_A1C1_1200.pn
g",
"summary": "Fresh bananas, perfect for a healthy snack.",
"title": "Bananas"
},
{
"id": 1,
"imageUrl":
"https://www.meijer.com/content/dam/meijer/product/0000/00/4133/00/0000004133000_1_A1C1_1200.pn
g",
"summary": "Crisp and sweet Gala apples.",
"title": "Gala Apples"
}
]
```

## Product Detail API (GET):

```
\- Sample Response with productid = 0:
{
"description": "Fresh bananas, perfect for a healthy snack. Rich in potassium and vitamins.",
"id": 0,
"imageUrl":
"https://www.meijer.com/content/dam/meijer/product/0000/00/4011/00/0000004011000_1_A1C1_1200.pn
g",
"price": "$0.59/lb",
"summary": "Fresh bananas, perfect for a healthy snack.",
"title": "Bananas"
}
```


## UI/UX Mockups:

List screen mockup: clicking on each of the list items takes the user to that products detail

screen.

Detail screen mockup:
