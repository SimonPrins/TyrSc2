#!/bin/bash
# on windown run it from gitbash (or mingw shell) like this: ./netcore-build.sh
# distributable build will be available in Tyr\bin\release\netcoreapp2.1\publish
dotnet build --framework netcoreapp2.1 -c release
dotnet publish --framework netcoreapp2.1 -c release