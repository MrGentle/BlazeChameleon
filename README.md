# BlazeChameleon
A cross-platform CLI which runs an API that interfaces with SteamWorks and SteamWeb. 

With this you can fetch any data that is available to a game at runtime(Lobbies, leaderboards, etc) and any data that is available from the SteamWeb API.

The main caveat being that you must have a logged in Steam client running in the same environment as the application to be able to access SteamWorks.

Please contact me if you find a good way to dockerize the application, or a way to run it with Steam in headless mode.


Feel free to contribute.

## Building
1. Clone the repository.
2. Add a static class named Config. Example:
```csharp
public static class Config {
    public const uint APP_ID = 553310; 
    public static string[] STEAM_WEB_API_KEYS = {"Key1", "Key2", ... };
}
```
3. Build for your target.
``dotnet build BlazeChameleon.csproj --runtime linux-x64``


## Running
Steam needs to be running and logged into to be able to interface with steamworks.
> BlazeChameleon --listen --port=8080

You can also supply the ``--secret`` argument to secure your routes with a password.

Use ``BlazeChameleon --help`` for more info


## Setting up your Ubuntu server with Steam
<details>
    <summary>Instructions</summary>
    Applies to Ubuntu 20.04
    I got this working on a Contabo VPS.

    Create a sudo user and switch to it
    ```
    adduser gentle
    usermod -aG sudo gentle
    su gentle
    ```

    Install xfce4
    ```
    sudo apt update
    sudo apt install xfce4 xfce4-goodies
    ```
    - Select lightdm as your manager during the installation

    Install tigervnc standalone and autocutsel for clipboard support
    ```
    sudo apt install tigervnc-standalone-server autocutsel
    ```

    Set a VNC password
    ```
    vncpasswd
    ```

    Configure TigerVNC
    ```
    nano ~/.vnc/xstartup
    ```
    - Paste this to your xstartup
    ```
    #!/bin/sh
    unset SESSION_MANAGER
    unset DBUS_SESSION_BUS_ADDRESS
    autocutsel -fork
    exec startxfce4 
    ```

    Make the startup file executable
    ```
    chmod u+x ~/.vnc/xstartup
    ```

    Create an additional file for any additional TigerVNC configuration
    ```
    geometry=1920x1080
    dpi=96
    ```

    Start and stop your VNC server
    ```
    vncserver
    vncserver -list
    vncserver -kill :1
    ```

    Create a service to autostart VNC on boot
    ```
    sudo nano /etc/systemd/system/vncserver@.service
    ```
    Paste and modify
    ```
    [Unit]
    Description=Remote desktop service (VNC)
    After=syslog.target network.target

    [Service]
    Type=simple
    User=gentle
    PAMName=login
    PIDFile=/home/%u/.vnc/%H%i.pid
    ExecStartPre=/bin/sh -c '/usr/bin/vncserver -kill :%i > /dev/null 2>&1 || :'
    ExecStart=/usr/bin/vncserver :%i -localhost no -geometry 1920x1080 -alwaysshared -fg
    ExecStop=/usr/bin/vncserver -kill :%i

    [Install]
    WantedBy=multi-user.target
    ```

    Enable the service
    ```
    sudo systemctl daemon-reload
    sudo systemctl enable vncserver@1.service
    sudo systemctl start vncserver@1.service
    sudo systemctl status vncserver@1.service
    ```

    Reboot and try connecting to your VNC server
    ```
    sudo reboot
    ```

    Once connected check if your xserver is configured correctly
    ```
    glxinfo -B
    ```
    If this outputs an error, try reinstalling from 0 or try another VNC server

    Time to install steam
    ```
    wget -O ~/steam.deb http://media.steampowered.com/client/installer/steam.deb
    sudo dpkg --install steam.deb
    sudo apt install wget gdebi-core libgl1-mesa-glx:i386
    ```

    Run steam
    ```
    steam
    ```

And hopefully you have a working VNC server running steam!
</details>