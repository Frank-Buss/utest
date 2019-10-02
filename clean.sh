#!/bin/sh

rm -rf Library Temp Logs obj
find . -name \*.csproj -type f -delete
find . -name \*.pidb -type f -delete
find . -name \*.unityproj -type f -delete
find . -name \*.DS_Store -type f -delete
find . -name \*.sln -type f -delete
find . -name \*.userprefs -type f -delete
