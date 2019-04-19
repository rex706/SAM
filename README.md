# SAM - Steam Account Manager

Easily log in and switch between Steam accounts.


![alt tag](http://i.imgur.com/7sUxieF.png)

[**DOWNLOAD**](https://github.com/rex706/SAM/releases)

**Features**

* 1-click Steam logins for multiple accounts.
* 2FA Support.
* Auto login a selected or the most recently used account.
* Encrypts saved user password.
* Adjust number of accounts per row.
* Import/Export accounts.
* Start with windows.
* Start minimized.
* Minimize to tray.
* Data password protection.

------------------------------------

**CHANGELOG**

**Latest version:** 
1.2.5.0
* Password protect application and data file.
	- Encrypts entire data file.
* Fix properly minimizing to system tray if setting enabled.
* Fix Steam Guard window detection in different languages using the [em dash '—'](https://en.wikipedia.org/wiki/Dash) instead of standard hyphen.
* Crash fixes.

1.2.4.1
* Steam Guard login fixes.
	- Additional Language support.
* Auto-login fixes when used with the Start With Windows option.
* Fix config.vdf parse path.
* Alias input to display instead of username.
* Tray right click options and logging in.
* Drag window while dragging background.
* Login freeze fixes.

1.2.4.0
* Multi-select accounts for export through the file drop down menu.
	- Can also export individual account through the context menu.
* Focus the 2FA window for every key press.
* Auto Steam path button in Settings, which will try to find the installed Steam path from the registry or installed directory for portable users.
* Utilize the Steam Web Api and parsing the Steam config.vdf with [Gameloop.vdf](https://github.com/shravan2x/Gameloop.Vdf) to try to automatically find profile information.
* Auto login and SAMSettings.ini fixes.
* Bundle new Updater.exe

1.2.3.2
* Better handle waiting for the 2FA Steam Guard window when logging in.

1.2.3.1
* Fixed an issue that would cause a crash if an old account did not have the new 2FA field, which all accounts with the previous update did not have.

1.2.3.0
* 2FA Support thanks to [gmmanonymus111](https://github.com/gmmanonymus111)
    - Your 'Shared Secret' can be found in your decrypted .maFile generated from [SteamDesktopAuthenticator](https://github.com/Jessecar96/SteamDesktopAuthenticator)
    - SAM will encrypt your 'Shared Secret' before saving it for future use like your password. 
* Update some NuGet Packages.

1.2.2.0
* Much better handling of profile image scrape.
	- No longer relies on image source pattern.
* Reload all images from edit drop down.
* Change Steam file path in settings.

1.2.1.0
* Fixed file export bug.
* Import and Export have been moved to the 'File' dropdown.

1.2.0.0
* Sort accounts alphabetically through the 'Edit' dropdown.
* Import/Export accounts data file in settings.
* Start minimized setting.
* Auto-Login now only triggers if Steam is not already open.
* Account buttons now change opacity when clicked.
* Redesign settings window.

1.1.0.1
* Fixed a bug that would not let emails be entered into the 'Name' field.

1.1.0.0

* Automatic login on program startup.
	- Most recently used account via settings menu.
	- Pre-selected account via new account/edit menu.
* Enable minimize.
* Do not display tooltip if description is empty/blank.
* Fixed saving and loading steam path.

1.0.2.1

* Start with windows setting. 

1.0.2.0

* Optional description profile entry.
* Confirmation window when deleting entries.

1.0.1.0

* Window resize animations
* Fix settings window to be less annoying

1.0.0.1

* 1.0 release (completely usable with all origionally planned features)
* Edit entry
* Bug fixes

0.1.4.1

* Bundle new autoupdater
* Fix window refresh bugs 
* Lighten background color
* Display version number in help menu or if there is an update

0.1.4.0

* Finally fixed obnoxious button glow effect.

0.1.3.1

* Fixed all spacing weirdness/issues.

0.1.3

* Display steam avatar on button if user inputs a profile url.
* Password entry masking.
* Switched to type List<T> instead of HashTable to store information.
 - Old info.dat will be deleted

0.1.2

* Right click on any account for an option to delete it.
* File menu.
 - Settings window.
* New row after user specified amount of accounts in settings. Defaults to 5.

0.1.1

* Release
