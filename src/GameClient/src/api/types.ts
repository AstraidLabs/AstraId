export type GameStateDto = {
  player: { playerId: string; userSub: string; phase: string; shieldUntilUtc?: string }
  resources: { energy: number; minerals: number; alloys: number; research: number; influence: number; unity: number }
  activeResearchProjectId?: string
  researchProgress: number
  graduationReady: boolean
}

export type GalaxyViewDto = {
  phase: string
  nodes: Array<{ systemId: string; x: number; y: number; starClass: string; known: boolean; owned: boolean }>
  edges: Array<{ a: string; b: string }>
  regions: string[]
}

export type GameSystemDto = {
  systemId: string
  name: string
  starType: string
  owned: boolean
  surveyProgress: number
  planets: Array<{ planetIndex: number; name: string; colonized: boolean; pop: number; biomes: string[]; habitability: number }>
}
