#!/bin/bash

WORKSPACE=..
LUBAN_DLL=$WORKSPACE/Luban/Luban.dll
CONF_ROOT=.

dotnet $LUBAN_DLL \
    -t server \
    -d csv \
    --conf $CONF_ROOT/luban.conf \
    -x outputDataDir=csv