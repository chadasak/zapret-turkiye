[🇹🇷 turkce](README.tr.md) · 🇬🇧 english

# zapret turkey edition

this is zapret, tuned for turkey.
tested mostly on roblox and discord. it also gets past the proton vpn block that isps put in place, at the udp wireguard level. i still cannot get past the block on plain wireguard.

note: it will not work in turkey if your dns is not set. this project cannot do its job properly without a different dns, so add at least one public dns to your active connection. i recommend 1.1.1.1 (backup 1.0.0.1).

short version
- add the dns in your wi-fi / ethernet settings: `1.1.1.1` (or backup `1.0.0.1`)
- run it as administrator.
- use `install.bat` to pin it in the background, or `zapret_bypass.bat` to run it by hand.
- if something breaks, look at `zapret.log`.

it does not only touch tcp traffic, it affects udp as well, which is what lets you use proton vpn. proton vpn currently only works on the udp wireguard setting.


# what the isp actually does

these came out of the test button in the tray app. same machine, minutes apart,
every target measured three times and the median taken:

```
                          zapret on    zapret off
cloudflare.com (control)  129 ms       137 ms
www.roblox.com            126 ms       connection reset
gamejoin.roblox.com       141 ms       connection reset
discord.com               136 ms       connection reset
gateway.discord.gg        133 ms       connection reset
api.protonvpn.ch          159 ms       11072 ms
protonvpn.com             157 ms       7798 ms
```

cloudflare.com is the control and it barely moves, 129 against 137. that matters
twice over. the connection itself is fine with zapret off, so nothing below is
just a broken line. and zapret costs nothing measurable on a site nobody is
blocking.

roblox and discord are properly blocked, and in the same way. the tcp connection
opens fine in about 150 ms. then the tls clienthello goes out and the connection
dies on the spot, every single time, 3 out of 3. the clienthello is the packet
that carries the site name in the clear. something in the middle reads that name
and forges a reset. the server is not closing anything, the thing in between is.
gateway.discord.gg dies too, which is the one that carries voice and messages,
so discord would not work even if the site somehow opened.

proton is a different animal. it is not blocked, it is throttled. the handshake
completes every time, but it takes 8 to 12 seconds instead of 0.16. packets get
dropped and tcp keeps retrying until enough get through. that is why the client
feels like it will not even open: every request it makes takes ten seconds and
times out. they did not even need to block it.

this is exactly what `--dpi-desync-split-pos=sniext+4` in the config is for. it
splits the clienthello right across the site name so the dpi cannot match it. the
numbers say it works.

# install
- do not forget to set the dns before installing the service; without it this may not work in turkey. recommended value: `1.1.1.1`.
- example command to add dns: `netsh interface ipv4 set dns name="Wi-Fi" static 1.1.1.1 primary`
- dns over https gives you extra protection.
- for one-off use, opening `zapret_bypass.bat` as administrator is enough.
- if you want it running in the background every time you turn the pc on, open `install.bat` as administrator. it sets up firewall and defender for you and runs it in the background as a windows task.
- to remove it, open `uninstall.bat` as administrator. it removes the windows tasks and puts the firewall and defender settings back the way they were.
- if anything goes wrong during install, remember to check the log file

# tray app

there is a `ZapretTray.exe` now. it sits next to your clock as a small z icon. double click it, it will ask for uac, that is normal, winws.exe needs administrator anyway.

you do not get it from this repo. it is not committed here on purpose. github
actions builds it from the source in `tray/` when a version is tagged, and
attaches it to the [releases](https://github.com/chadasak/zapret-turkiye/releases)
page, so the build log shows exactly which commit produced the binary you
downloaded.

every release carries the sha256. check it before you run anything:

```powershell
Get-FileHash ZapretTray.exe -Algorithm SHA256
```

it asks for uac and it adds a defender exclusion for its own folder, because
winws.exe needs both. that is precisely why you should not have to take anyone's
word for what is inside it.

left click opens a panel, right click opens a menu. the panel has a big start/stop button. white icon means on, dim grey means off, you can tell at a glance.

what it does:
- on and off. it reads the settings from `zapret_task.cmd`, so the config still lives there. change that file and the app follows, you are not keeping the same thing in two places.
- start with windows switch. same job as `install.bat`, it creates the task.
- show the icon at logon switch. turn this on and it comes back on every boot without asking for uac.
- if winws.exe crashes it restarts it by itself. if it tried 3 times in 2 minutes and it still will not start, it gives up and tells you, so it never loops forever.
- dns guard. it checks whether you have a public dns and whether dns is encrypted. it does not just read the setting, it sends a real query to see if doh actually works. because if doh gets blocked, windows quietly drops to plaintext and nothing in the registry changes. if that happens it warns you. zapret cannot help you there anyway, you are getting the wrong ip in the first place.
- repair button. firewall rule, defender exclusion, dns cache, file unblocking. it does all of them one after another.
- test button. it connects to roblox, proton, discord and cloudflare as a control, and measures the tls handshake. if the connection gets reset it says BLOCKED, if it takes longer than 3 seconds it says THROTTLED. if cloudflare fails too it tells you the problem is your internet, not censorship. you get to look instead of guessing.

quitting it stops zapret too, opening it brings zapret back. so the icon really is an on/off switch.

the log shows up inside the panel as well, no need to open notepad.

if you want to build it yourself, `tray/build.cmd`. it compiles with the csc.exe that already ships inside windows, you do not need to install an sdk. it produces a single file.

# update
when zapret gets an update, all you do is replace what is inside the `bin` folder. after that run `install.bat` as administrator again. that file already removes the zapret service, clears the dns cache and creates a service again from scratch. you do not need to do remove -> install a second time.
note: zapret will only get bugfix updates from now on.

# vpns
the isp i am on right now blocks proton vpn during the handshake. the udp parts in this zapret config are there to get past that block and the censorship. even stealth mode in proton vpn's windows client does not work, but with this zapret config the `UDP WireGuard` setting started working.

# zapret config for people using it outside windows
```bash
  --wf-tcp=80,443 ^
  --dpi-desync=fake,split2 ^
  --dpi-desync-autottl=2 ^
  --dpi-desync-fooling=md5sig ^
  --dpi-desync-split-pos=sniext+4 ^
  --dpi-desync-repeats=2 ^
  --new ^
  --wf-udp=51820 ^
  --dpi-desync=fake ^
  --dpi-desync-repeats=2 ^
  --dpi-desync-any-protocol ^
  --dpi-desync-cutoff=d2 ^
  --dpi-desync-autottl=2
```
