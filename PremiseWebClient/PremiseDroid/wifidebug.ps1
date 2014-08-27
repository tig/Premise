$toolspath = $env:LOCALAPPDATA + "\Android\android-sdk\platform-tools"
$adb = $toolspath + "\adb.exe"
& $adb tcpip 4455
& $adb connect 192.168.0.108:4455