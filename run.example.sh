#!/bin/bash

mono XG.Server.Cmd.exe > log.txt & pid=$!
echo $pid > pid

