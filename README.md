# EternSynth

Aplicación de escritorio moderna y ligera para la síntesis y generación procedimental de efectos de sonido retro (8 bits). Incluye preajustes rápidos para videojuegos, control detallado de parámetros de osciladores y filtros, visualización de onda en tiempo real y exportación directa a formato WAV nativo.

## Características

- **Preajustes de Sonido Rápidos**: Genera sonidos de Moneda, Disparo, Explosión, Powerup, Salto, Blip/Selección y Mutaciones aleatorias de forma instantánea.
- **Controlador de Onda**: Soporta ondas cuadradas, sierra, senoidales, ruido y triangulares.
- **Parámetros del Sintetizador**: Ajustes de envolvente de volumen, frecuencia de tono, vibrato, arpegios, phaser/flanger, filtros paso-bajo/paso-alto y velocidad de repetición.
- **Visualizador Reactivo**: Dibuja la onda resultante en tiempo real en la pantalla.
- **Historial de Sonidos**: Panel lateral que guarda tus sonidos locales favoritos para edición posterior.
- **Menú Superior Inteligente**: Barra de menús autocomprimible al pasar el cursor (hover) para mantener el espacio de trabajo limpio.
- **Zoom / Escala Dinámica**: Ajusta la escala de toda la interfaz desde un control deslizante del 60% al 130%.
- **Acciones Rápidas**: Copiar y pegar parámetros al portapapeles, guardar parámetros a archivos `.json`/`.txt` y exportar a archivos `.wav` de 16 bits.

## Estructura del Proyecto

- `SfxrSynth.cs`: Motor matemático y síntesis de audio retro.
- `WpfMainWindow.cs`: Interfaz de usuario nativa en WPF C#.
- `WpfVectorIcons.cs`: Diccionario de iconos vectoriales SVG nativos.
- `compile.ps1`: Script de automatización de compilación nativa en PowerShell.

## Cómo Compilar y Ejecutar

1. Abre una consola de PowerShell en el directorio del proyecto.
2. Ejecuta el script de compilación:
   ```powershell
   .\compile.ps1
   ```
3. Ejecuta `EternSynth.exe` resultante.
