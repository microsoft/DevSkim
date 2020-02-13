@IF EXIST "%~dp0\node.exe" (
  "%~dp0\node.exe"  "./main.js" %*
) ELSE (
  @SETLOCAL
  @SET PATHEXT=%PATHEXT:;.JS;=;%
  node  "./main.js" %*
)