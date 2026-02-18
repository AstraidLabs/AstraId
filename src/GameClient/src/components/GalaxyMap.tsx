import { Application, Container, Graphics } from 'pixi.js'
import { useEffect, useRef } from 'react'
import type { GalaxyViewDto } from '../api/types'

export function GalaxyMap({ galaxy, onSelect }: { galaxy?: GalaxyViewDto; onSelect: (systemId: string) => void }) {
  const hostRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!hostRef.current || !galaxy) return
    const app = new Application()
    const container = new Container()
    let dragging = false
    let lx = 0
    let ly = 0

    app.init({ width: hostRef.current.clientWidth || 800, height: 500, background: '#020617', antialias: true }).then(() => {
      hostRef.current!.innerHTML = ''
      hostRef.current!.appendChild(app.canvas)
      app.stage.addChild(container)

      galaxy.edges.forEach((edge) => {
        const a = galaxy.nodes.find((x) => x.systemId === edge.a)
        const b = galaxy.nodes.find((x) => x.systemId === edge.b)
        if (!a || !b) return
        const g = new Graphics().moveTo(a.x + 450, a.y + 250).lineTo(b.x + 450, b.y + 250).stroke({ width: 1, color: 0x334155 })
        container.addChild(g)
      })

      galaxy.nodes.forEach((node) => {
        const color = node.owned ? 0x22c55e : node.known ? 0x38bdf8 : 0x475569
        const g = new Graphics().circle(node.x + 450, node.y + 250, node.owned ? 5 : 3).fill(color)
        g.eventMode = 'static'
        g.cursor = 'pointer'
        g.on('pointertap', () => onSelect(node.systemId))
        container.addChild(g)
      })

      app.canvas.addEventListener('wheel', (event) => {
        event.preventDefault()
        const delta = event.deltaY < 0 ? 1.1 : 0.9
        container.scale.set(Math.max(0.4, Math.min(3, container.scale.x * delta)))
      })

      app.canvas.addEventListener('mousedown', (event) => {
        dragging = true
        lx = event.clientX
        ly = event.clientY
      })

      app.canvas.addEventListener('mousemove', (event) => {
        if (!dragging) return
        container.x += event.clientX - lx
        container.y += event.clientY - ly
        lx = event.clientX
        ly = event.clientY
      })

      window.addEventListener('mouseup', () => { dragging = false })
    })

    return () => {
      app.destroy(true)
    }
  }, [galaxy, onSelect])

  return <div className="h-[500px] w-full rounded border border-slate-700" ref={hostRef} />
}
