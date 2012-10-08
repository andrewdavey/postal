msbuild /p:Configuration=Release /p:TargetFrameworkVersion=v4.0 /t:Rebuild src\postal\postal.csproj

msbuild /p:Configuration=Release /p:TargetFrameworkVersion=v4.5 /t:Rebuild src\postal\postal.csproj 

pushd .
cd nuget

rmdir /S /Q lib

mkdir lib
mkdir lib\net40
mkdir lib\net45

copy ..\src\postal\bin\release\v4.0\postal.dll lib\net40
copy ..\src\postal\bin\release\v4.5\postal.dll lib\net45

..\src\.nuget\nuget.exe pack postal.nuspec

popd