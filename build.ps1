$ErrorActionPreference = 'Stop'

#
# dotnet build isn't used because it can't build net35 tfms
#

msbuild -v:m -restore -t:Build -p:Configuration=Release -p:TargetFramework=net35 de4dot.netframework.sln
if ($LASTEXITCODE) { exit $LASTEXITCODE }
Remove-Item Release\net35\*.pdb, Release\net35\*.xml, Release\net35\Test.Rename.*

msbuild -v:m -restore -t:Build -p:Configuration=Release -p:TargetFramework=net45 de4dot.netframework.sln
if ($LASTEXITCODE) { exit $LASTEXITCODE }
Remove-Item Release\net45\*.pdb, Release\net45\*.xml, Release\net45\Test.Rename.*
