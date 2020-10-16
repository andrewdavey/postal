# https://github.com/NuGet/Home/issues/8580#issuecomment-555696372
for f in  src/Postal.AspNetCore/bin/Release/*.nupkg
do
  curl -vX PUT -u "hermanho:$GH_TOKEN" -F package=@$f https://nuget.pkg.github.com/hermanho/
done

dotnet nuget push src/Postal.AspNetCore/bin/Release/*.nupkg --source https://www.myget.org/F/herman-github/api/v2/package --api-key $NUGET_API_KEY