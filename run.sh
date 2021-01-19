docker run --name patrons-web-api -v $HOME/.aws/credentials:/root/.aws/credentials -v $HOME/.aws/config:/root/.aws/config --rm --network cluster patrons-web-api -d
