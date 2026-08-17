/// YAML front matter helpers (ported from SiteFi's Main.fs).
module Blogo.Yaml

open System
open System.Text.RegularExpressions
open YamlDotNet.Serialization

let SplitIntoHeaderAndContent(source: string) =
  let delimRE =
    Regex("^---\\w*\r?$", RegexOptions.Compiled ||| RegexOptions.Multiline)

  let searchFrom = if source.StartsWith("---") then 3 else 0

  let m = delimRE.Match(source, searchFrom)

  if m.Success then
    source.[searchFrom .. m.Index - 1], source.[m.Index + m.Length ..]
  else
    "", source

let OfYaml<'T>(yaml: string) =
  let deserializer = (new DeserializerBuilder()).Build()

  if String.IsNullOrWhiteSpace yaml then
    deserializer.Deserialize<'T>("{}")
  else
    deserializer.Deserialize<'T>(yaml)
