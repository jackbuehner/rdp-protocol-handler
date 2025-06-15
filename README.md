# Remote Desktop Protocol Handler for Windows

This adds support for opening remote desktops and apps via the rdp:// URL scheme/protocol.

Specifically, this app will read the all text after rdp:// in a URL, convert it to a temporary RDP file, and then launch mstsc.exe with the RDP file. Text after rdp:// should be in the format of URL-encoded query string parameters.

Example: rdp://full%20address=s:192.168.0.20&username=s:jackbuehner

## Get the latest published release

<a href="https://apps.microsoft.com/detail/9n1192wschv9" target="_blank">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://get.microsoft.com/images/en-us%20light.svg">
    <source media="(prefers-color-scheme: light)" srcset="https://get.microsoft.com/images/en-us%20dark.svg">
    <img src="frontend/lib/assets/favorites_light.png" alt="A screenshot of the favorites page in RAWeb">
  </picture>
</a>

Old versions can be found on [the releases page](https://github.com/jackbuehner/rdp-protocol-handler/releases).
