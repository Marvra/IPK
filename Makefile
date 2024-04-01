PROJECT_FILE = ipk24chat_client.csproj
BUILD_NAME = ipk24chat_client

build:
	dotnet build ipk24chat_client.csproj -p:PublishSingleFile=true --property WarningLevel=0 --self-contained true -c Release -o ./project
	mv ./project/$(BUILD_NAME) ./
# chmod +x $(BUILD_NAME)
	

