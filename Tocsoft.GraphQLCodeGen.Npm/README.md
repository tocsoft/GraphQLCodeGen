
tocsoft.graphql-codegen is a tool chain for compiling graphql queries into client code.

> This module required .net installed on your system, either .NET 4.6.1+ or .NET Core 3.1.

## Installation

    npm install tocsoft.graphql-codegen --save-dev

## Configuration

Generation config is derived from discovering a `gqlsettings.json` file in your directory tree relative to a graphql file you are trying to process.

### `gqlsettings.json`
    {
        "format": "ts",
        "schema": "schema.json",
        "root":true
    }
#### `format`
format can be on of `ts` or `cs`.

`ts` for typescript generation and `cs` for c# generation. (for c# please use the [Tocsoft.GraphQLCodeGen.MsBuild](https://www.nuget.org/packages/Tocsoft.GraphQLCodeGen.MsBuild/) for an improved experience)

#### `schema`

`schema` should point to a graphql schema, it can be in either json format (as derived from the standard introspection query) or it can point to a schema written in the standard graphql syntax.

> Paths are relative to the file they are in.

#### `root`

`root` can be `true` or `false`(default value if omitted). What this does is stops the generator from walking and further up the directory tree looking for more configuration.

## Usage

    tocsoft-graphql-codegen --format ts ./**/*.gql

This discovers all the files ending `.gql` and processes them, by default this command it will generate (for each `*.gql` file) a match`*.gql.ts` next to the original. 

## More advanced usage

For advanced usage check out our [wiki](https://github.com/tocsoft/GraphQLCodeGen/wiki), or ask over on our [discussions page](https://github.com/tocsoft/GraphQLCodeGen/discussions).