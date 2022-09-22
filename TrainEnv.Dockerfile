
# use the official gpu-empowered TensorFlow image
FROM tensorflow/tensorflow:2.9.1-gpu

# install dotnet 7.0 to run the schafkopf training (C#)
ARG DOTNET_TAR_FILE=dotnet-sdk-7.0.100-rc.1.22431.12-linux-x64.tar.gz
ARG DOTNET_PROD_BIN=bf594dbb-5ec8-486b-8395-95058e719e1c/42e8bc351654ed4c3ccaed58ea9180a1
ARG DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/$DOTNET_PROD_BIN/$DOTNET_TAR_FILE
ARG DOTNET_INSTALL_DIR=/opt/dotnet
ADD $DOTNET_URL .
RUN mkdir $DOTNET_INSTALL_DIR && tar xzf $DOTNET_TAR_FILE -C $DOTNET_INSTALL_DIR
ENV PATH=$DOTNET_INSTALL_DIR:$PATH

ENTRYPOINT ["dotnet", "run", \
    "--project", "Schafkopf.Training/Schafkopf.Training.csproj", \
    "--configuration", "Release"]
