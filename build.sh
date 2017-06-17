dotnet --info
dotnet restore
dotnet test test/Cap.Consistency.EntityFrameworkCore.Test/Cap.Consistency.EntityFrameworkCore.Test.csproj -f netcoreapp1.1
dotnet test test/Cap.Consistency.Test/Cap.Consistency.Test.csproj -f netcoreapp1.1