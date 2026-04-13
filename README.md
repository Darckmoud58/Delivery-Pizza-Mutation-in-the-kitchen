# 🍕 Delivery Pizza - Mutation in the Kitchen

## 📖 Descripción
Juego cooperativo multijugador 2D desarrollado en Unity. Dos jugadores deben trabajar juntos para destruir el **Horno Mutante** que genera enemigos en la cocina, antes de que los enemigos acaben con ellos.

## 🎮 Modos de Juego
- **Cooperativo en red** — 2 jugadores conectados via Photon PUN2
- **Roles:**
  - 👨‍🍳 **Chef** — Jugador 1 (Master Client)
  - 🛵 **Repartidor** — Jugador 2

## 🕹️ Controles
| Acción | Tecla |
|--------|-------|
| Moverse | WASD / Flechas |
| Atacar | J |

## ⚔️ Mecánicas
- Los jugadores atacan enemigos y el **Horno Mutante**
- El Horno tiene **3 fases** según su vida:
  - 🟢 **Normal (100%-61%)** — Genera 1 enemigo cada 5 seg
  - 🟠 **Dañado (60%-31%)** — Genera 1 enemigo cada 3 seg
  - 🔴 **Crítico (30%-1%)** — Genera 2 enemigos cada 1.5 seg
- Los enemigos persiguen y atacan al jugador más cercano
- HUD con barra de vida del Horno con cambio de color por fase

## 🛠️ Tecnologías
- Unity 2D
- C#
- Photon PUN2 (Multijugador en red)

## 👥 Equipo
- Darckmoud58
- MarioGlez58

## 🏫 Universidad
UTJ — Programación de Videojuegos II | 8vo Semestre — 2026
