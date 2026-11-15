# YA-NEC 0.5.1 - esito del collaudo automatico (collaudo_automatico.bat, richiesta di IC8TEM del 15/11/2026 16:35).
# Legge il resoconto del collaudo, scrive ESITO.txt (SUPERATO o NON SUPERATO, riepilogo delle fasi, passi da guardare) e lo aggiunge
# allo zip del collaudo. Codice d'uscita: 0 = superato, 1 = non superato o incompleto, 2 = resoconto mancante.
param([string]$Rep = 'collaudo_completo.txt', [string]$Uscita = 'ESITO.txt', [string]$Zip = '', [string]$Versione = '')
if (-not (Test-Path -LiteralPath $Rep)) {
    Set-Content -LiteralPath $Uscita -Encoding UTF8 -Value @('ESITO DEL COLLAUDO AUTOMATICO DI YA-NEC ' + $Versione, '', 'NON SUPERATO: manca il resoconto ' + $Rep)
    exit 2
}
$righe = @(Get-Content -LiteralPath $Rep -Encoding UTF8)
# riepilogo: dall'ultima riga "FASE 3 ... RIEPILOGO" in poi
$ir = -1
for ($k = 0; $k -lt $righe.Count; $k++) { if ($righe[$k] -match 'FASE 3' -and $righe[$k] -match 'RIEPILOGO') { $ir = $k } }
$riep = @()
# 0.5.2 (collaudo 0.5.1): il riepilogo sono le righe tra il titolo della fase 3 e la riga "Inizio ... fine ..." (prima si perdevano le righe
# con un solo punto, "Collaudo delle funzioni . superato"); i confronti sono sensibili alle maiuscole (-cmatch): "(avvisi solo per le parti
# che il .ez non contiene)" della 2H non e' un "NON" e non deve far fallire il collaudo
if ($ir -ge 0) {
    for ($k = $ir + 1; $k -lt $righe.Count; $k++) {
        if ($righe[$k] -cmatch '^Inizio ') { break }
        if ($righe[$k].Trim() -ne '') { $riep += $righe[$k] }
    }
}
$male = @($riep | Where-Object { $_ -cmatch 'NON ' -or $_ -cmatch 'CON ERRORI' -or $_ -cmatch 'ERRORE' -or $_ -cmatch 'non eseguit' -or $_ -cmatch 'non riuscit' })
# passi da guardare: la sezione scritta dal programma
$passi = @()
$ip = -1
for ($k = 0; $k -lt $righe.Count; $k++) { if ($righe[$k] -match 'PASSI DA GUARDARE') { $ip = $k } }
if ($ip -ge 0) {
    for ($k = $ip + 1; $k -lt $righe.Count; $k++) {
        if ($righe[$k] -match '^=====' -or $righe[$k] -match '^----- ') { break }
        $passi += $righe[$k]
    }
}
$nErr = @($righe | Where-Object { $_ -cmatch 'esito:\s+ERRORE' }).Count
$nAvv = @($righe | Where-Object { $_ -cmatch 'esito:\s+AVVISO' }).Count
$ok = ($ir -ge 0) -and ($riep.Count -gt 0) -and ($male.Count -eq 0) -and ($nErr -eq 0)
$out = @()
$out += 'ESITO DEL COLLAUDO AUTOMATICO DI YA-NEC ' + $Versione + ' - ' + (Get-Date -Format 'dd/MM/yyyy HH:mm')
$out += ''
if ($ok) { $out += 'SUPERATO: tutte le fasi sono andate bene. Il pacchetto da pubblicare viene preparato in pubblicazione\ (non pubblicato).' }
elseif ($ir -lt 0) { $out += 'NON SUPERATO: il collaudo non e arrivato al riepilogo (interrotto?).' }
else { $out += 'NON SUPERATO: ' + $male.Count + ' fasi non riuscite o non eseguite, ' + $nErr + ' passi con ERRORE. Il pacchetto da pubblicare NON viene preparato.' }
$out += ('Passi con ERRORE: ' + $nErr + ' - passi con AVVISO: ' + $nAvv)
$out += ''
$out += 'RIEPILOGO DELLE FASI'
$out += $riep
if ($male.Count -gt 0) { $out += ''; $out += 'FASI DA GUARDARE'; $out += $male }
$out += ''
$out += 'PASSI DA GUARDARE (AVVISO o ERRORE)'
if ($passi.Count -gt 0) { $out += $passi } else { $out += '(nessuno)' }
$out += ''
$out += 'Mandare yanec-collaudo-' + $Versione + '.zip (contiene anche questo file).'
Set-Content -LiteralPath $Uscita -Encoding UTF8 -Value $out
if ($Zip -ne '' -and (Test-Path -LiteralPath $Zip)) {
    try { Compress-Archive -LiteralPath $Uscita -DestinationPath $Zip -Update } catch { }
}
if ($ok) { exit 0 } else { exit 1 }
