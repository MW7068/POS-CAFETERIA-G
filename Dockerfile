FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/dominio/PosCafeteria.Dominio.csproj src/dominio/
COPY src/infraestructura/PosCafeteria.Infraestructura.csproj src/infraestructura/
COPY tests/PosCafeteria.Tests/PosCafeteria.Tests.csproj tests/PosCafeteria.Tests/

RUN dotnet restore tests/PosCafeteria.Tests/PosCafeteria.Tests.csproj

COPY src/dominio/ src/dominio/
COPY src/infraestructura/ src/infraestructura/
COPY tests/PosCafeteria.Tests/ tests/PosCafeteria.Tests/

RUN dotnet test tests/PosCafeteria.Tests/PosCafeteria.Tests.csproj -c Release --no-restore -- RunConfiguration.TreatNoTestsAsError=true

CMD ["dotnet", "test", "tests/PosCafeteria.Tests/PosCafeteria.Tests.csproj", "-c", "Release", "--no-build", "--", "RunConfiguration.TreatNoTestsAsError=true"]
