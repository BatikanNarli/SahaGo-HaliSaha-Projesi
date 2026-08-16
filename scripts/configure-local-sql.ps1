$ErrorActionPreference = 'Stop'

$tcpRoot = 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQLServer\SuperSocketNetLib\Tcp'

Set-ItemProperty -LiteralPath $tcpRoot -Name Enabled -Type DWord -Value 1
Set-ItemProperty -LiteralPath $tcpRoot -Name ListenOnAllIPs -Type DWord -Value 0

foreach ($ipKey in 'IP11', 'IP12') {
    $ipPath = Join-Path $tcpRoot $ipKey
    Set-ItemProperty -LiteralPath $ipPath -Name Enabled -Type DWord -Value 1
    Set-ItemProperty -LiteralPath $ipPath -Name TcpDynamicPorts -Value ''
    Set-ItemProperty -LiteralPath $ipPath -Name TcpPort -Value '1433'
}

Restart-Service -Name MSSQLSERVER -Force
$deadline = (Get-Date).AddSeconds(60)

do {
    Start-Sleep -Milliseconds 500
    $service = Get-Service -Name MSSQLSERVER
} until ($service.Status -eq 'Running' -or (Get-Date) -gt $deadline)

if ($service.Status -ne 'Running') {
    throw 'MSSQLSERVER did not return to Running state within 60 seconds.'
}
