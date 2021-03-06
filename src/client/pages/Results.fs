module Pages.Results

open PropertyMapper.Contracts
open Fable.Helpers.React
open Fable.Helpers.React.Props

type SearchResults = { SearchTerm : SearchTerm; Response : SearchResponse }
type Model = { SearchResults : SearchResults option; Selected : PropertyResult option }

type Msg =
    | FilterSet of facet:string * value: string
    | DisplayResults of SearchTerm * SearchResponse
    | ChangePage of int
    | SelectTransaction of PropertyResult
    | SetPostcode of string
let init _ : Model = { SearchResults = None; Selected = None }

let view model dispatch =
    let toTh c = th [ Scope "col" ] [ str c ]
    let toDetailsLink row c =
        td [ Scope "row" ] [
            a [ Href "#"
                DataToggle "modal"
                unbox ("data-target", "#exampleModal")
                OnClick(fun _ -> dispatch (SelectTransaction row))
                 ] [ str c ]
        ]
    let toTd c = td [ Scope "row" ] [ str c ]
    div [ ClassName "container-fluid border rounded m-3 p-3 bg-light" ] [
        yield model.Selected |> Option.map Details.view |> Option.defaultValue (div [] [])
        match model.SearchResults with
        | None -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Please perform a search!" ] ] ]
        | Some { Response = { Results = [||] } } -> yield div [ ClassName "row" ] [ div [ ClassName "col" ] [ h3 [] [ str "Your search yielded no results." ] ] ]
        | Some { SearchTerm = term; Response = response } ->
            let hits = response.TotalTransactions |> Option.map (commaSeparate >> sprintf " (%s hits)") |> Option.defaultValue ""
            let description =
                match term with
                | Term term -> sprintf "Search results for '%s'%s." term hits
                | Postcode postcode -> sprintf "Showing properties within a 1km radius of '%s'%s." postcode hits

            yield div [ ClassName "row" ] [
                div [ ClassName "col-2" ] [ Pages.Filter.createFilters (FilterSet >> dispatch) response.Facets ]
                div [ ClassName "col-10" ] [
                    div [ ClassName "row" ] [ div [ ClassName "col" ] [ h4 [] [ str description ] ] ]
                    table [ ClassName "table table-bordered table-hover" ] [
                        thead [] [
                            tr [] [ toTh "Street"
                                    toTh "Town"
                                    toTh "Postcode"
                                    toTh "Date"
                                    toTh "Price" ]
                        ]
                        tbody [] [
                            for row in response.Results ->
                                tr [] [ toDetailsLink row (row.Address.Building + " " + (row.Address.Street |> Option.defaultValue ""))
                                        toTd row.Address.TownCity
                                        td [ Scope "row" ] [ a [ Href "#"; OnClick(fun _ -> row.Address.PostCode |> Option.iter(SetPostcode >> dispatch)) ] [ row.Address.PostCode |> Option.defaultValue "" |> str ] ]
                                        toTd (row.DateOfTransfer.ToShortDateString())
                                        toTd (sprintf "£%s" (commaSeparate row.Price)) ]
                        ]                
                    ]
                    nav [] [
                        ul [ ClassName "pagination" ] [
                            let buildPager enabled content current page =
                                li [ ClassName ("page-item" + (if enabled then "" else " disabled") + (if current then " active" else "")) ] [
                                    button [ ClassName "page-link"; Style [ Cursor "pointer" ]; OnClick (fun _ -> dispatch (ChangePage page)) ] [ str content ]
                                ]
                            let currentPage = response.Page
                            let totalPages = int ((response.TotalTransactions |> Option.defaultValue 0 |> float) / 20.)
                            yield buildPager (currentPage > 0) "Previous" false (currentPage - 1)
                            yield!
                                [ for page in 0 .. totalPages ->
                                    buildPager true (string (page + 1)) (page = currentPage) page ]
                            yield buildPager (currentPage < totalPages) "Next" false (currentPage + 1)
                        ]
                    ]
                ]
            ]
    ]

let update msg model : Model =
    match msg with
    | FilterSet _ | ChangePage _ | SetPostcode _ -> model
    | DisplayResults (term, response) -> { SearchResults = Some { SearchTerm = term; Response = response }; Selected = None }
    | SelectTransaction transaction -> { model with Selected = Some transaction }