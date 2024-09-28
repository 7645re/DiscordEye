$env:VAULT_ADDR="http://localhost:8200"
$env:VAULT_TOKEN="root-token"

function Get-RandomString {
    param (
        [int]$length = 8
    )
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    -join (Get-Random -Count $length -InputObject $chars.ToCharArray())
}

function Get-RandomIPAddress {
    return "$([math]::floor((Get-Random) * 255)).$([math]::floor((Get-Random) * 255)).$([math]::floor((Get-Random) * 255)).$([math]::floor((Get-Random) * 255))"
}

$proxyCount = 10

for ($i = 1; $i -le $proxyCount; $i++) {
    $id = [guid]::NewGuid()
    $address = Get-RandomIPAddress
    $port = Get-Random -Minimum 9000 -Maximum 9999
    $login = Get-RandomString -length 6
    $password = Get-RandomString -length 8

    $vaultCommand = "vault kv put secret/proxy/$id id=`"$id`" address=`"$address`" port=`"$port`" login=`"$login`" password=`"$password`""

    docker exec vault sh -c $vaultCommand
}