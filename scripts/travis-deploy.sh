dotnet nuget add source "https://nuget.pkg.github.com/hermanho/index.json" --name "GitHub" --username hermanho --password $GH_TOKEN
dotnet nuget push src/Postal.AspNetCore/bin/Release/*.nupkg --source "GitHub"
dotnet nuget push src/Postal.AspNetCore/bin/Release/*.nupkg --source https://www.myget.org/F/herman-github/api/v2/package --api-key $NUGET_API_KEY