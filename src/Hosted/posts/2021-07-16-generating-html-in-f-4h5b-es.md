---
title: Generando HTML en F#
subtitle: ~
categories: fsharp,templates,html,dotnet
abstract: Cosas simples en fsharp El dia de hoy vamos a ver formas de producir archivos HTML con F#...
date: 2021-07-16
language: es
---

[Scriban]: https://github.com/scriban/scriban
[Giraffe.ViewEngine]: https://giraffe.wiki/view-engine
[Feliz.ViewEngine]: https://github.com/dbrattli/Feliz.ViewEngine
[Fable]: https://fable.io/docs/
[Giraffe.Razor]: https://github.com/giraffe-fsharp/Giraffe.Razor

## Cosas simples en fsharp

Hola!, esta es la cuarta entrada en "Cosas simples en F#"

Hoy hablaremos acerca de como podemos producir cadenas de texto que contienen HTML en F# (utiles para reportes, pdfs o similares). Producir HTML en F# es muy sencillo para ser honesto, hay muchas librerias que te permiten hacerlo, hablaremos de algunas que he usado con anterioridad.

- [Giraffe.ViewEngine]
- [Feliz.ViewEngine]
- [Scriban]

Los primeros dos son <abbr title="Domain Specific Language">DSL</abbr>'s para construir HTML, el ultimo es un lenguaje de scripting para .NET (aunque tambien existe [Giragge.Razor] pero el dia de hoy no lo mencionare)

### Giraffe

En cuanto a versiones (sabores como me gusta decirle) de DSL para HTML en F# Giraffe es la mas tradicional y probablemente el _sabor_ menos apreciado hoy en dia por la comunidad. Sin embargo eso no disminuye su utilidad, Giraffe.ViewEngine utiliza nodos de XML como su bloque _lego_ central, es decir puedes producir tanto HTML como XML (lo cual puede ser util en algunos contextos asi que puntos extras).

La estructura de cada etiqueta de este DSL es la siguiente:

- `etiqueta [(* lista de atributos *)] [(* lista de nodos *)]`

por ejemplo para crear `<div class="mi-clase"></div>` seria algo como esto:

- `div [ _class "mi-clase" ] []`

> Los atributos tienen un prefijo de un guion bajo `_` para prevenir choques con palabras reservadas de F# (como class y type).

Vamos a ver una pagina sencilla con un _header_
```fsharp
#r "nuget: Giraffe.ViewEngine"

open Giraffe.ViewEngine


let view =
    html [] [
        head [] [ title [] [ str "Giraffe" ] ]
        body [] [ header [] [ str "Giraffe" ] ]
    ]
let document = RenderView.AsString.htmlDocument view

printfn "%s" document
```

> Para correr esto, copia el contenido en algun archivo con terminacion ***.fsx*** (como `script.fsx`) y teclea en la terminal:
> - `dotnet fsi run script.fsx`
> ***NOTA***: para esto es necesario tener instalado el [SDK de .NET](https://dotnet.microsoft.com/download)

En este caso, asignamos los resultados de la funcion de html a la variable _`vista`_, luego renderizamos el contenido como una cadena de texto. Si has seguido esta serie de posts, te daras una idea de que puedes usar las funciones del _namespace_ `System.IO` para producir un archivo de HTML con ese contenido

> Si estas usando Saturn/Giraffe como tu framework web, no es necesario hacer el render manual, estos frameworks tienen funciones utiltarias que se encargan de ello (como lo veremos en el siguiente post).

Tambien puedes crear funciones para _pre-definir_ aspectos de tus vistas o incluso sobre-escribir valores en caso de que lo consideres necesario, por ejemplo:


```fsharp
#r "nuget: Giraffe.ViewEngine"

open Giraffe.ViewEngine


let card attributes =
    article [ yield! attributes; _class "card is-green"]

let cardFooter attributes =
    footer [ yield! attributes; _class "card-footer is-rounded"]

let cardHeader attributes =
    header [ yield! attributes; _class "card-header no-icons"]

let mySection = 
    div [] [
        card [ _id "my-card" ] [
            cardHeader [] [
                h1 [] [ str "This is my custom card"]
                img [ _src "https://some-image.com"; _class "card-header-image" ]
            ]

            p [] [ str "this is the body of the card" ]

            cardFooter [ _data "my-attr" "extra attributes" ] [
                p [] [ str "This is my footer"]
            ]
        ]
    ]

// notese que usamos htmlNode en esta ocasion en lugar de htmlDocument
let document = RenderView.AsString.htmlNode mySection

printfn "%s" document
```
> Para correr esto, copia el contenido en algun archivo con terminacion ***.fsx*** (como `script.fsx`) y teclea en la terminal:
> - `dotnet fsi run script.fsx`
> ***NOTA***: para esto es necesario tener instalado el [SDK de .NET](https://dotnet.microsoft.com/download)

> **Nota**: yield! es una palabra reservada que asigna los contenidos de alguna secuencia (`IEnumerable<T>`) a la secuencia actual.
> Por ejemplo: `let a = [1;2;3]` `let b = [yield! a; 4;5;6]`, `b` ahora contiene `[1;2;3;4;5;6]`

Asi que para crear nuevas _etiquetas_ colo es necesario hacer una funcion nueva que acepte atributos y nodos!

### Feliz

El DSL original de Feliz fue hecho por [Zaid Ajaj](https://twitter.com/zaid_ajaj) (Quien por cierto, produce EXCELENTE contenido OSS en F#, deberian revisar su perfil de github una maravilla 🤌🏼) para ser usado en aplicaciones de [Fable] que para fines practicos es un DSL encima del React.js si revisamos en el editor, veremos que `view` es de tipo `ReactElement` Pero no te preocupes, no hay Javascript aqui, solo es el tipo de dato, el DSL de Feliz mejora mucho la legibilidad y reduce la cantidad de caracteres que debemos escribir para producir HTML tambien en el backend, no solo en el frontend.

> No soy el master en composicion de vistas con Feliz/React asi que toma el siguiente ejemplo con su correspondiente granito de arena, en lo personal te recomendaria que revisaras el [Libro de Elmish](https://zaid-ajaj.github.io/the-elmish-book/) que contiene patrones precisamente para esta clase de casos.

```fsharp
#r "nuget: Feliz.ViewEngine"

open Feliz.ViewEngine

let view = 
    Html.html [
        Html.head [ Html.title "Feliz" ]
        Html.body [
            Html.header [ prop.text "Feliz" ]
        ]
    ]

let document = Render.htmlDocument view

printfn "%s" document
```
> Para correr esto, copia el contenido en algun archivo con terminacion ***.fsx*** (como `script.fsx`) y teclea en la terminal:
> - `dotnet fsi run script.fsx`
> ***NOTA***: para esto es necesario tener instalado el [SDK de .NET](https://dotnet.microsoft.com/download)


Cuando abres el _namespace_ `Feliz.ViewEngine` tienes acceso a las clases estaticas `Html` y `prop`, estas contienen las etiquetas y los atributos que puedes necesitar para construir HTML, si te sientes cansado de escribir `Html.etiqueta` y `prop.atributo` puedes usar `open type Html` y `open type prop` que funcionan como si abrieras estas como si fuera un _namespace_.

```fsharp
#r "nuget: Feliz.ViewEngine"

open Feliz.ViewEngine
// notese el open type
open type Html
open type prop

let view =
    // ya no tuvimos que escribir Html.html!
    html [
        head [ title "Feliz" ]
        body [
            header [ text "Feliz" ]
        ]
    ]

let document = Render.htmlDocument view

printfn "%s" document
```
> Para correr esto, copia el contenido en algun archivo con terminacion ***.fsx*** (como `script.fsx`) y teclea en la terminal:
> - `dotnet fsi run script.fsx`
> ***NOTA***: para esto es necesario tener instalado el [SDK de .NET](https://dotnet.microsoft.com/download)

usando `open type` nos salvamos de unos cuantos tecleos, pero podemos tener algunos problemas de choques con los nombres, en casos como esos, siempre puedes volver a escribir `Html.etiqueta` y `prop.atributo` en caso de ser necesario.

Vamos a continuar con el componente **Card** para ver como podemos componer nuestras vistas (como si fueran legos) con diferentes elementos (componentes) en si. En el caso de Feliz, `children` es una propiedad para alojar a los hijos de algun componente padre y como F# es un lenguaje de tipado estricto no puedes combinar diferentes tipos de datos asi que tendremos que pasar el contenido a `children` en lugar de ponerlo todo en props.

```fsharp
#r "nuget: Feliz.ViewEngine"

open Feliz.ViewEngine
open type Html
open type prop

// card personalizada, no puedes personalizar sus clases, pero si sus hijos
let card (content: ReactElement seq) = 
    article [
        className "card is-green"
        children content
    ]
// usando yield! podemos colocar cualquier otra propiedad (tanto children como cualquier `prop.*`)
let cardFooter content =
    footer [
        className "card-footer is-rounded"
        yield! content
    ]

let slotedHeader (content: ReactElement seq) = 
    header [
        className "card-header"
        // pasamos el contenido directo a los hijos
        children content
    ]

let customizableHeader content = 
    header [
        className "card-header"
        // hagase garras y ponga lo que quiera
        yield! content
    ]

let card1 = 
    div [
        card [
            // solo permitimos hijos, mas no propiedades
            slotedHeader [
                h1 [ text "This is my custom card"]
                // className "" nel, no se arma
            ]
            p [ text "this is the body of the card" ]
            cardFooter [
                custom("data-my-attr", "extra attributes")
                children (p [text "This is my footer"])
            ]
        ]
    ]

let card2 = 
    div [
        card [
            // En este caso nuestro encabezado personalizado nos permite
            // pasar propiedades asi como elementos
            customizableHeader [
                children (h1 [ text "This is my custom card"])
                className "custom class" 
            ]
            p [ text "this is the body of the card" ]
            cardFooter [
                custom("data-my-attr", "extra attributes")
                children (p [text "This is my footer"])
            ]
        ]
    ]

let r1 = Render.htmlView card1
let r2 = Render.htmlView card2

printfn "%s\n\n%s" r1 r2
```
> Para correr esto, copia el contenido en algun archivo con terminacion ***.fsx*** (como `script.fsx`) y teclea en la terminal:
> - `dotnet fsi run script.fsx`
> ***NOTA***: para esto es necesario tener instalado el [SDK de .NET](https://dotnet.microsoft.com/download)

Pero... aqui va a suceder algo interesante, a diferencia de `Giraffe.ViewEngine`, Feliz no remueve propiedades existentes, asi que nuestro header va a quedar algo asi:

```html
<header class="card-header" class="custom class">
    <h1>This is my custom card</h1>
</header>
```
Y en el caso de HTML, la ultima pripiedad siempre gana, asi que ten esto en cuenta cuando quieras sobre-escribir algo o algo  no se vea como deberia. Tambien puede ser un factor determinante de como es que podrias usar `yield!`.

### Scriban

Si tu, como yo no puedes simplemente deshacerte de HTML por que... _pos por que si_, entonces [Scriban] es una excelente alternativa para ti, ya que te permite escribir HTML como en la mayoria de lenguajes para templates existentes (handlebars, mustache, liquid templates, por ejemplo) y solo necesitas llenarlo de datos al final

```fsharp
#r "nuget: Scriban"

open Scriban

type Product = { name: string; price: float; description: string }

let renderProducts products = 
    let html = 
        """
        <ul id='products'>
        {{ for product in products }}
          <li>
            <h2>{{ product.name }}</h2>
                 Price: {{ product.price }}
                 {{ product.description | string.truncate 15 }}
          </li>
        {{ end }}
        </ul>
        """
    let result = Template.Parse(html)
    result.Render({| products = products |})

let result =
    renderProducts [
        { name = "Zapatos"; price = 20.50; description = "Los Zapatos mas Zapatezcos que veras..."}
        { name = "Papas"; price = 1.50; description = "Las papas mas papezcas que veras..." }
        { name = "Cars"; price = 10.3; description = "Los Carros mas Carrozos que veras..." }
    ]

printfn "%s" result
```
> Para correr esto, copia el contenido en algun archivo con terminacion ***.fsx*** (como `script.fsx`) y teclea en la terminal:
> - `dotnet fsi run script.fsx`
> ***NOTA***: para esto es necesario tener instalado el [SDK de .NET](https://dotnet.microsoft.com/download)

Si has usado Jinja, moustache, handlebars, como lo mencionaba la sintaxis te sera similar, basicamente solo tienes que definir HTML y hacerle un parsing/render con una fuente de datos (en caso de que uses variables en el template)

> Scriban tiene un monton de [utilidades](https://github.com/scriban/scriban/blob/master/doc/builtins.md) en su lenguaje de scripting (como esos pipes `|` para truncar el texto)

En caso de que quieras componer vistas en Scriban, el acercamiento que se tiene que dar es muy, muy diferente.

```fsharp
#r "nuget: Scriban"

open System
open Scriban

type Product = 
    { name: string;
      price: float; 
      details : {| description: string |} }
// crea un fragmento/componente en una cadena de texto con html
let detailDiv = 
    """
    <details>
        <summary> {{ product.details.description | string.truncate 15 }} <summary>
        {{ product.details.description }}
    </details>
    """

let renderProducts products = 
    let html = 
        /// aqui usamos una funcion de F# `sprintf`
        /// y {{ "%s" | object.eval_template }}
        /// en este caso usamos F# para pre procesar ese template
        sprintf
            """
            <ul id='products'>
            {{ for product in products }}
              <li>
                <h2>{{ product.name }}</h2>
                     Price: {{ product.price }}
                     {{ "%s" | object.eval_template }}
              </li>
            {{ end }}
            </ul>
            """
            detailDiv
    let result = Template.Parse(html)
    result.Render({| products = products |})

let result =
    renderProducts [
        { name = "Zapatos"
          price = 20.50
          details = 
            {| description = "Los Zapatos mas Zapatezcos que veras..." |} }
        { name = "Papas"
          price = 1.50
          details =
            {| description = "Las papas mas papezcas que veras..."  |} }
        { name = "Carros"
          price = 10.3
          details =
            {| description = "Los Carros mas Carrozos que veras..."  |} }
    ]

printfn "%s" result
```
> Para correr esto, copia el contenido en algun archivo con terminacion ***.fsx*** (como `script.fsx`) y teclea en la terminal:
> - `dotnet fsi run script.fsx`
> ***NOTA***: para esto es necesario tener instalado el [SDK de .NET](https://dotnet.microsoft.com/download)

Ahora, toma en cuenta que en este caso usamos F# para pre-procesar el template antes de ahora si renderizar el template con Scriban, lo cual es propenso a problemas por que un string aqui, otro alla, y ya nos perdimos lo del maracuya, en casos como estos es simplemente mas sencillo que escribas tus archivos HTML y le dejes el resto a Scriban y el modelo que le pases para que pueda hacer lo que esta hecho para hacer. Si ya conoces como es template y los requerimientos son claros probablemente este sea el mejor acercamiento de los tres en mi opinion.

Ahora, ten en cuenta de que si lo necesitas, puedes pasar multiples fragmentos de HTML en cadenas de texto y hacer una especie de layout (como en los patrones MVC) y renderizar todo al final, aunque no me consta que tanto pueda incurrir en el desenoeño (aunque Scriban en su github dicen que son extremadamente veloces)

## Closing Thoughts

Asi que ahi lo tienen! HTML en F#, facil y sencillo no? espero que les haya sido de utilidad, quiza pueden usar esto para alguna utilidad de consola para convertir JSON a HTML! o quiza para el proximo reporte de las ventas del año pasado o algun otro caso de uso similar.

Si tienes dudas o preguntas, buscame en [Twitter](https://twitter.com/angel_d_munoz)!