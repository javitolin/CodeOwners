FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt update && apt install -y iputils-ping git