import { useEffect, useMemo, useState } from 'react'
import { GameApiClient } from '../api/client'
import type { GalaxyViewDto, GameStateDto, GameSystemDto } from '../api/types'
import { userManager } from '../auth/oidc'
import { GalaxyMap } from '../components/GalaxyMap'

const api = new GameApiClient(import.meta.env.VITE_APP_SERVER_URL ?? 'https://localhost:7003', async () => (await userManager.getUser())?.access_token ?? null)

export function GamePage() {
  const [state, setState] = useState<GameStateDto>()
  const [galaxy, setGalaxy] = useState<GalaxyViewDto>()
  const [system, setSystem] = useState<GameSystemDto>()
  const [selectedTab, setSelectedTab] = useState<'state' | 'research' | 'planets'>('state')

  useEffect(() => {
    api.state().then(setState)
    api.galaxy().then(setGalaxy)
  }, [])

  const selectedSystemTitle = useMemo(() => system ? `${system.name} (${system.starType})` : 'No system selected', [system])

  const sendCommand = async (type: string, payload: Record<string, unknown>) => {
    await api.command({ commandId: crypto.randomUUID(), type, payload })
    setState(await api.state())
    if (system) setSystem(await api.system(system.systemId))
  }

  return <div className="grid h-full grid-cols-[220px_1fr_320px] bg-slate-950 text-slate-100">
    <aside className="border-r border-slate-800 p-4">
      <div className="mb-4 text-xl font-semibold">AstraID Idle Galaxy</div>
      <button className="mb-2 block w-full rounded bg-slate-800 px-3 py-2" onClick={() => setSelectedTab('state')}>State</button>
      <button className="mb-2 block w-full rounded bg-slate-800 px-3 py-2" onClick={() => setSelectedTab('research')}>Research</button>
      <button className="block w-full rounded bg-slate-800 px-3 py-2" onClick={() => setSelectedTab('planets')}>Planets</button>
      <button className="mt-8 rounded border border-slate-600 px-3 py-2" onClick={() => userManager.signoutRedirect()}>Logout</button>
    </aside>

    <main className="p-4">
      <header className="mb-3 rounded border border-slate-800 p-3 text-sm">
        <div>Player: {state?.player.userSub}</div>
        <div>Phase: {state?.player.phase} {state?.player.shieldUntilUtc ? `(Shield until ${state.player.shieldUntilUtc})` : ''}</div>
      </header>
      <GalaxyMap galaxy={galaxy} onSelect={async (id) => setSystem(await api.system(id))} />
    </main>

    <section className="border-l border-slate-800 p-4">
      <h2 className="mb-3 text-lg font-semibold">{selectedSystemTitle}</h2>
      {selectedTab === 'state' && <div className="space-y-1 text-sm">
        <div>⚡ Energy: {state?.resources.energy.toFixed(1)}</div>
        <div>⛏ Minerals: {state?.resources.minerals.toFixed(1)}</div>
        <div>🛡 Alloys: {state?.resources.alloys.toFixed(1)}</div>
        <div>🔬 Research: {state?.resources.research.toFixed(1)}</div>
        <div>⭐ Influence: {state?.resources.influence.toFixed(1)}</div>
      </div>}
      {selectedTab === 'research' && <div className="space-y-2 text-sm">
        <div>Project: {state?.activeResearchProjectId ?? 'none'}</div>
        <div>Progress: {state?.researchProgress.toFixed(1)}</div>
        <button className="rounded bg-indigo-600 px-3 py-2" onClick={() => sendCommand('StartResearch', { projectId: 'ftl-theory' })}>Start FTL Research</button>
      </div>}
      {selectedTab === 'planets' && <div className="space-y-2 text-sm">
        {system?.planets.map((p) => <div key={p.planetIndex} className="rounded border border-slate-700 p-2">
          <div className="font-medium">{p.name}</div>
          <div>Habitability: {(p.habitability * 100).toFixed(0)}%</div>
          <div>Biomes: {p.biomes.join(', ')}</div>
          <button className="mt-2 rounded bg-emerald-700 px-2 py-1" onClick={() => sendCommand('ColonizePlanet', { systemId: system.systemId, planetIndex: p.planetIndex })}>Colonize</button>
        </div>)}
      </div>}

      {system && <div className="mt-4 space-y-2">
        <button className="rounded bg-cyan-700 px-3 py-2" onClick={() => sendCommand('StartSurvey', { systemId: system.systemId })}>Survey system</button>
        <button className="rounded bg-amber-700 px-3 py-2" onClick={() => sendCommand('BuildOutpost', { systemId: system.systemId })}>Build outpost</button>
      </div>}
    </section>
  </div>
}
