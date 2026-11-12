# Usa a chave do dispositivo (include/segredos.h) como senha da gravação pela rede,
# a mesma que o programa configura no ArduinoOTA. Assim a senha não fica repetida em dois lugares.
import pathlib
import re

Import("env")  # noqa: F821 (fornecido pelo PlatformIO)

segredos = pathlib.Path(env.subst("$PROJECT_INCLUDE_DIR"), "segredos.h")  # noqa: F821
if segredos.exists():
    achou = re.search(r'#define\s+CHAVE_DISPOSITIVO\s+"([^"]+)"', segredos.read_text(encoding="utf-8"))
    if achou:
        env.Append(UPLOADERFLAGS=["--auth=" + achou.group(1)])  # noqa: F821
