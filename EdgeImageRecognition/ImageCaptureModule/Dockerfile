FROM microsoft/dotnet:2.0-sdk AS build-env
WORKDIR /app

COPY avicap32.dll /app/
COPY avifil32.dll /app/
COPY msacm32.dll /app/
COPY msvfw32.dll /app/
COPY opencv_ffmpeg341_64.dll /app/
COPY OpenCvSharpExtern.dll /app/

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.0-runtime
WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "ImageCaptureModule.dll"]