To compile the app run the following commands (powershell):

```bash

1. Initialize a new Git repository:
```bash
mkdir sample
cd sample
git init
```
2. Add the remote repository
```bash
git remote add origin https://github.com/KrzysztofDusko/JustyBase.git
```
3. Enable sparse-checkout in negative mode:
```bash
git config core.sparseCheckout true
```
4. Create sparse-checkout file and add excluded paths with !:
```bash
echo "/*" >> .git/info/sparse-checkout
echo "!source/JustyBase" >> .git/info/sparse-checkout
echo "!pictures" >> .git/info/sparse-checkout
echo "!Help" >> .git/info/sparse-checkout
echo "!source/Benchmarks" >> .git/info/sparse-checkout
echo "!source/JustyBase.slnx" >> .git/info/sparse-checkout
```

5. Pull the repository:
```bash
git pull origin master 
```

6. Publish the project:

```bash
cd ..
dotnet publish sample/source/LibraryUsageSamples/JustyBase.Database.Sample/JustyBase.Database.Sample.csproj -o . && rm *.pdb
Rename-Item -Path "JustyBase.Database.Sample.exe" -NewName "AvaloniaClipboard.exe"
```


7. Remove sample repository:
```bash
rm -r sample -Force
```