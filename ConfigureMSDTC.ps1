# ————————————
# Enable MSDTC
# ————————————

Write-Host "Enabling MSDTC..." -ForegroundColor Yellow
$DTCSecurity = "Incoming"
$RegPath = "HKLM:\SOFTWARE\Microsoft\MSDTC\"

#Set Security and MSDTC path

$RegSecurityPath = "$RegPath\Security"

Set-ItemProperty –path $RegSecurityPath –name "NetworkDtcAccess" –value 1
Set-ItemProperty –path $RegSecurityPath –name "NetworkDtcAccessClients" –value 1
Set-ItemProperty –path $RegSecurityPath –name "NetworkDtcAccessTransactions" –value 1
Set-ItemProperty –path $RegSecurityPath –name "NetworkDtcAccessInbound" –value 1
Set-ItemProperty –path $RegSecurityPath –name "NetworkDtcAccessOutbound" –value 1
Set-ItemProperty –path $RegSecurityPath –name "LuTransactions" –value 1

if ($DTCSecurity –eq "None")
{
	Set-ItemProperty –path $RegPath –name "TurnOffRpcSecurity" –value 1
	Set-ItemProperty –path $RegPath –name "AllowOnlySecureRpcCalls" –value 0
	Set-ItemProperty –path $RegPath –name "FallbackToUnsecureRPCIfNecessary" –value 0
}
elseif ($DTCSecurity –eq "Incoming")
{
	Set-ItemProperty –path $RegPath –name "TurnOffRpcSecurity" –value 0
	Set-ItemProperty –path $RegPath –name "AllowOnlySecureRpcCalls" –value 0
	Set-ItemProperty –path $RegPath –name "FallbackToUnsecureRPCIfNecessary" –value 1
}
else
{
	Set-ItemProperty –path $RegPath –name "TurnOffRpcSecurity" –value 0
	Set-ItemProperty –path $RegPath –name "AllowOnlySecureRpcCalls" –value 1
	Set-ItemProperty –path $RegPath –name "FallbackToUnsecureRPCIfNecessary" –value 0
}

Restart-Service MSDTC
Write-Host "——MSDTC has been configured—–" –foregroundcolor green
