@echo off
echo Creating and entering sample directory...
mkdir sample
cd sample

echo Initializing git repository...
git init

echo Adding remote repository...
git remote add origin https://github.com/KrzysztofDusko/JustyBase.git

echo Configuring sparse checkout...
git config core.sparseCheckout true

echo Setting up sparse-checkout patterns...
echo /* > .git/info/sparse-checkout
echo !source/JustyBase >> .git/info/sparse-checkout
echo !pictures >> .git/info/sparse-checkout
echo !Help >> .git/info/sparse-checkout
echo !source/Benchmarks >> .git/info/sparse-checkout
echo !source/JustyBase.slnx >> .git/info/sparse-checkout

echo Pulling repository...
git pull origin master

echo Publishing project...
cd ..
dotnet publish sample/source/LibraryUsageSamples/JustyBase.Database.Sample/JustyBase.Database.Sample.csproj -o .

if exist *.pdb del /F /Q *.pdb

if exist JustyBase.Database.Sample.exe ren JustyBase.Database.Sample.exe AvaloniaClipboard.exe

echo Cleaning up...
rmdir /S /Q sample

echo Done!
pause
