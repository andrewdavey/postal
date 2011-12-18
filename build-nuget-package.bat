msbuild /p:Configuration=Release src\postal\postal.csproj

pushd .
cd nuget

rmdir /S /Q lib

mkdir lib
mkdir lib\net40

copy ..\src\postal\bin\release\postal.dll lib\net40

..\src\.nuget\nuget.exe pack postal.nuspec

popd