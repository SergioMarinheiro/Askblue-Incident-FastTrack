// AskBlue logo: white wordmark on royal blue. The picture has a lot of blue around the wordmark,
// so it is zoomed in (scale-150) and the edges are cut off by the box (overflow-hidden).
// Imported (not referenced by URL), so Vite bundles it and adds a content hash to its file name.
import askblueLogo from '@/assets/askblue_bg_royalblue.jpg'

export function Logo() {
  return (
    <div className="flex items-center gap-3">
      <div className="h-9 w-20 shrink-0 overflow-hidden rounded-lg">
        <img src={askblueLogo} alt="AskBlue" className="size-full scale-150 object-cover" />
      </div>
      <div className="leading-tight">
        <div className="font-semibold text-white">Incident</div>
        <div className="text-sm text-white/70">FastTrack</div>
      </div>
    </div>
  )
}
