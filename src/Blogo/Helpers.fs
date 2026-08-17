/// Small shared helpers (ported from SiteFi's Main.fs).
module Blogo.Helpers

open System
open System.Globalization
open System.IO
open System.Text.RegularExpressions

let NULL_TO_EMPTY(s: string) =
  match s with
  | null -> ""
  | t -> t

let FORMATTED_DATE(dt: DateTime) = dt.ToString("MMM dd, yyyy")

/// RFC 3339 timestamp for Atom (dates are treated as UTC).
let ATOM_DATE(dt: DateTime) =
  dt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)

/// RFC 822 timestamp for RSS 2.0 (GMT zone token, English day/month names).
let RSS_DATE(dt: DateTime) =
  dt.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture)

/// Return (fullpath, filename-without-extension, (year, month, day), slug, extension)
let (|ArticleFile|_|)(fullpath: string) =
  let I s = Int32.Parse s
  let filename = Path.GetFileName(fullpath)

  let filenameWithoutExt = Path.GetFileNameWithoutExtension(fullpath)

  let r = new Regex("^([0-9]+)-([0-9]+)-([0-9]+)-(.+)\.(md)")

  let r2 =
    new Regex("^([1-2][0-9][0-9][0-9])([0-1][0-9])([0-3][0-9])-(.+)\.(md)")

  if r.IsMatch(filename) then
    let a = r.Match(filename)
    let V(i: int) = a.Groups.[i].Value
    Some(fullpath, filenameWithoutExt, (I(V 1), I(V 2), I(V 3)), V 4, V 5)
  elif r2.IsMatch(filename) then
    let a = r2.Match(filename)
    let V(i: int) = a.Groups.[i].Value
    Some(fullpath, filenameWithoutExt, (I(V 1), I(V 2), I(V 3)), V 4, V 5)
  else
    None
