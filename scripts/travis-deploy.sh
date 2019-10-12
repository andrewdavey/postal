nuget source add -Name "GitHub" -Source "https://nuget.pkg.github.com/hermanho/index.json" -UserName hermanho -Password $GH_TOKEN
nuget push src/Postal.AspNetCore/bin/Release/*.nupkg -Source "GitHub"
dotnet nuget push src/Postal.AspNetCore/bin/Release/*.nupkg --source https://www.myget.org/F/herman-github/api/v2/package --api-key $NUGET_API_KEY