$address = "185.68.186.170"
$port = "8000"
$login = "dCYhMg"
$password = "4vYT8K"

for ($i = 1; $i -le 4; $i++) {
    $id = [guid]::NewGuid()
    
    $vaultCommand = "vault kv put secret/proxy/$id id=`"$id`" address=`"$address`" port=`"$port`" login=`"$login`" password=`"$password`""
    docker exec vault sh -c $vaultCommand
}