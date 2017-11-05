#!/usr/bin/env node
"use strict";

// get the args that shoul dbe passed over to the package
var args = process.argv.splice(2, process.argv.length - 2).map(function (a) { return a.indexOf(" ") === -1 ? a : `"${a}"` }).join(" ")


var hasFullDotNet = false;
var fs = require('fs');
if (process.env["windir"]) {
    try {
        var stats = fs.lstatSync(process.env["windir"] + '/Microsoft.NET');
        if (stats.isDirectory())
            hasFullDotNet = true;
    }
    catch (e) {
        console.log(e);
    }
}

var c = require('child_process');

//we need to checkif we are ruynning form a packages/installed version or source

var isDevMode = true;
if (fs.existsSync(__dirname + '/binaries')) {
    //we are in a released mode
    isDevMode = false;
}

var binaryPath = "";
if (hasFullDotNet) {
    if (isDevMode) {
        binaryPath = __dirname + '../Tocsoft.GraphQLCodeGen.Cli/bin/debug/net46/Tocsoft.GraphQLCodeGen.Cli.exe';
    } else {
        binaryPath = __dirname + '/binaries/net46/Tocsoft.GraphQLCodeGen.Cli.exe';
    }
} else {
    if (isDevMode) {
        binaryPath = __dirname + '../Tocsoft.GraphQLCodeGen.Cli/bin/debug/netcoreapp1.0/Tocsoft.GraphQLCodeGen.Cli.dll';
    } else {
        binaryPath = __dirname + '/binaries/netcoreapp1.0/Tocsoft.GraphQLCodeGen.Cli.dll';
    }
}

if (hasFullDotNet) {
    c.execSync(`"${binaryPath}" ${args}`, { stdio: [0, 1, 2] });
} else {
    c.execSync(`dotnet "${binaryPath}" ${args}`, { stdio: [0, 1, 2] });
}