/// URL scheme for the generated site.
/// "legacy" mode: identical to what the WebSharper sitelet generator produced
/// (ported verbatim from SiteFi's Main.fs Urls module).
module Blogo.Urls

open System

let CATEGORY (cat: string) lang =
  if String.IsNullOrEmpty lang then
    sprintf "/category/%s.html" cat
  else
    sprintf "/category/%s/%s.html" cat lang

let POST_URL(user: string, slug: string) =
  if String.IsNullOrEmpty user then
    sprintf "/blog/%s.html" slug
  else
    sprintf "/user/%s/%s.html" user slug

let USER_URL user =
  if String.IsNullOrEmpty user then
    sprintf "/user.html"
  else
    sprintf "/user/%s.html" user

let LANG(lang: string) =
  sprintf "/%s" (if lang <> "" then (sprintf "%s.html" lang) else "")
