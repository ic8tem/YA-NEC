# Automatic update

Every 3 minutes (and 10 seconds after start) YA-NEC silently checks the latest version published on this GitHub. If it is newer:

1. it downloads the package for its platform, its `.sha256` checksum and, from 1.7.2, its `.sig` signature;
2. it verifies the checksum and the YA-NEC signature (ECDSA P-256, public key built into the program): if either is missing or wrong, nothing is installed;
3. it puts the new files in place of the old ones: the new version starts **at the next launch**, with a message in the status bar.

The previous version is kept in `data/aggiornamenti/precedente-<version>`, to go back by hand. Settings (`data/config.ini`), logs and user models are not touched. If several YA-NEC windows are open, only the first one checks.

It can be turned off in **Settings > Update**; the **Force the update** button does not wait 3 minutes. Every step goes into the log (category AGGIORNAMENTO).

**.deb and .rpm packages**, when available: they install with administrator rights and are updated only with the package manager (`apt`, `dnf`, `zypper`). In those packages automatic update is off, and Settings says so.
