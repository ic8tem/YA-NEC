#!/usr/bin/env python3
# YA-NEC - descrizioni per i gestori di pacchetti di Windows (winget, Scoop, Chocolatey), generate a ogni Release dal workflow.
# Uso: genera.py <versione> <cartella dist con i file della Release> <cartella di uscita>
# L'invio ai registri (winget-pkgs, Scoop bucket, community.chocolatey.org) NON e' automatico: lo decide IC8TEM.
import hashlib, json, os, sys
ver, dist, out = sys.argv[1], sys.argv[2], sys.argv[3]
R = 'https://github.com/ic8tem/YA-NEC/releases/download/v%s/' % ver
def sha(n): return hashlib.sha256(open(os.path.join(dist, n), 'rb').read()).hexdigest().upper()
setup = {a: 'YA-NEC-%s-win-%s-setup.exe' % (ver, a) for a in ('x64', 'x86', 'arm64')}
zipw = {a: 'YA-NEC-%s-win-%s.zip' % (ver, a) for a in ('x64', 'x86', 'arm64')}
os.makedirs(out, exist_ok=True)
desc_en = 'NEC-2 wire antenna simulator (C# translation of nec2c) with calculators, optimizer, receiving antennas and dish design. By Costantino Cerrotta IC8TEM.'
# --- winget (manifest 1.6, tre file)
w = os.path.join(out, 'winget', 'manifests', 'i', 'IC8TEM', 'YA-NEC', ver); os.makedirs(w, exist_ok=True)
open(os.path.join(w, 'IC8TEM.YA-NEC.yaml'), 'w').write(
    'PackageIdentifier: IC8TEM.YA-NEC\nPackageVersion: %s\nDefaultLocale: en-US\nManifestType: version\nManifestVersion: 1.6.0\n' % ver)
ins = ''.join('  - Architecture: %s\n    InstallerUrl: %s%s\n    InstallerSha256: %s\n' % ({'x64': 'x64', 'x86': 'x86', 'arm64': 'arm64'}[a], R, setup[a], sha(setup[a])) for a in setup)
open(os.path.join(w, 'IC8TEM.YA-NEC.installer.yaml'), 'w').write(
    'PackageIdentifier: IC8TEM.YA-NEC\nPackageVersion: %s\nInstallerType: inno\nScope: user\nInstallModes:\n  - interactive\n  - silent\n'
    'UpgradeBehavior: install\nMinimumOSVersion: 10.0.0.0\nInstallers:\n%sManifestType: installer\nManifestVersion: 1.6.0\n' % (ver, ins))
open(os.path.join(w, 'IC8TEM.YA-NEC.locale.en-US.yaml'), 'w').write(
    'PackageIdentifier: IC8TEM.YA-NEC\nPackageVersion: %s\nPackageLocale: en-US\nPublisher: Costantino Cerrotta IC8TEM\n'
    'PublisherUrl: https://github.com/ic8tem\nPackageName: YA-NEC\nPackageUrl: https://github.com/ic8tem/YA-NEC\nLicense: GPL-3.0\n'
    'LicenseUrl: https://github.com/ic8tem/YA-NEC/blob/main/LICENSE\nShortDescription: NEC-2 wire antenna simulator\nDescription: %s\n'
    'Tags:\n  - antenna\n  - nec\n  - ham-radio\n  - yagi\nManifestType: defaultLocale\nManifestVersion: 1.6.0\n' % (ver, desc_en))
# --- Scoop (versione portatile dallo zip, data/ conservata tra un aggiornamento e l'altro)
sc = {'version': ver, 'description': desc_en, 'homepage': 'https://github.com/ic8tem/YA-NEC', 'license': 'GPL-3.0-only',
      'architecture': {'64bit': {'url': R + zipw['x64'], 'hash': sha(zipw['x64']).lower()},
                       '32bit': {'url': R + zipw['x86'], 'hash': sha(zipw['x86']).lower()},
                       'arm64': {'url': R + zipw['arm64'], 'hash': sha(zipw['arm64']).lower()}},
      'extract_dir': 'YA-NEC', 'bin': 'YA-NEC.exe', 'shortcuts': [['YA-NEC.exe', 'YA-NEC']], 'persist': 'data',
      'checkver': 'github', 'autoupdate': {'architecture': {
          '64bit': {'url': 'https://github.com/ic8tem/YA-NEC/releases/download/v$version/YA-NEC-$version-win-x64.zip'},
          '32bit': {'url': 'https://github.com/ic8tem/YA-NEC/releases/download/v$version/YA-NEC-$version-win-x86.zip'},
          'arm64': {'url': 'https://github.com/ic8tem/YA-NEC/releases/download/v$version/YA-NEC-$version-win-arm64.zip'}}}}
os.makedirs(os.path.join(out, 'scoop'), exist_ok=True)
json.dump(sc, open(os.path.join(out, 'scoop', 'ya-nec.json'), 'w'), indent=4)
# --- Chocolatey (sorgenti del pacchetto: choco pack nella cartella)
c = os.path.join(out, 'chocolatey'); os.makedirs(os.path.join(c, 'tools'), exist_ok=True)
open(os.path.join(c, 'ya-nec.nuspec'), 'w').write('''<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>ya-nec</id>
    <version>%s</version>
    <title>YA-NEC</title>
    <authors>Costantino Cerrotta IC8TEM</authors>
    <owners>IC8TEM</owners>
    <projectUrl>https://github.com/ic8tem/YA-NEC</projectUrl>
    <licenseUrl>https://github.com/ic8tem/YA-NEC/blob/main/LICENSE</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <projectSourceUrl>https://github.com/ic8tem/YA-NEC</projectSourceUrl>
    <docsUrl>https://github.com/ic8tem/YA-NEC/wiki</docsUrl>
    <tags>ya-nec antenna nec ham-radio yagi simulator</tags>
    <summary>NEC-2 wire antenna simulator</summary>
    <description>%s</description>
  </metadata>
  <files><file src="tools\\**" target="tools" /></files>
</package>
''' % (ver, desc_en))
open(os.path.join(c, 'tools', 'chocolateyinstall.ps1'), 'w').write('''$ErrorActionPreference = 'Stop'
$args = @{
  packageName    = 'ya-nec'
  fileType       = 'exe'
  url            = '%s%s'
  checksum       = '%s'
  checksumType   = 'sha256'
  url64bit       = '%s%s'
  checksum64     = '%s'
  checksumType64 = 'sha256'
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /ALLUSERS'
  validExitCodes = @(0)
}
Install-ChocolateyPackage @args
''' % (R, setup['x86'], sha(setup['x86']), R, setup['x64'], sha(setup['x64'])))
print('descrizioni scritte in', out)
