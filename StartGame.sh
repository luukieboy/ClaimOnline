#!/bin/bash

echo "Updating game..."
git checkout removeDoubleFiles
git pull origin main

echo "Launching game..."
./Build/LinuxClaim.x86_64