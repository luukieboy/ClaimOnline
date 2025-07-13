#!/bin/bash

echo "Updating game..."
git checkout removeDoubleFiles
git pull origin main

echo "Launching game..."
# chmod +x LinuxClaim.x86_64
./Builds/LinuxClaim.x86_64