#!/usr/bin/env bash
# Build index.html to browse pick frames in a grid (open in browser).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DIR="${1:-}"

if [[ -z "$DIR" ]]; then
  echo "Usage: generate_pick_gallery.sh <picks_folder>" >&2
  echo "Example: generate_pick_gallery.sh assets/card_portraits_src/picks/basic_attack" >&2
  exit 1
fi

if [[ "$DIR" != /* ]]; then
  DIR="$ROOT/$DIR"
fi

if [[ ! -d "$DIR" ]]; then
  echo "error: not a directory: $DIR" >&2
  exit 1
fi

OUT="$DIR/index.html"
TITLE="$(basename "$DIR")"
README=""
if [[ -f "$DIR/README.txt" ]]; then
  README="$(sed 's/&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g' "$DIR/README.txt")"
fi

FILES=()
while IFS= read -r line; do
  FILES+=("$line")
done < <(find "$DIR" -maxdepth 1 -name '*.png' | sort)

{
  cat <<EOF
<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${TITLE} — 选帧</title>
  <style>
    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: system-ui, -apple-system, sans-serif;
      background: #111;
      color: #eee;
    }
    header {
      position: sticky;
      top: 0;
      z-index: 10;
      padding: 12px 16px;
      background: #1a1a1a;
      border-bottom: 1px solid #333;
    }
    h1 { margin: 0 0 6px; font-size: 1.1rem; }
    .hint { font-size: 0.85rem; color: #aaa; }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 12px;
      padding: 16px;
    }
    figure {
      margin: 0;
      background: #1e1e1e;
      border: 2px solid #333;
      border-radius: 8px;
      overflow: hidden;
      cursor: pointer;
      transition: border-color 0.15s, transform 0.15s;
    }
    figure:hover { border-color: #666; transform: translateY(-2px); }
    figure.selected { border-color: #4a9eff; box-shadow: 0 0 0 2px #4a9eff44; }
    img {
      width: 100%;
      height: auto;
      display: block;
      aspect-ratio: 16/10;
      object-fit: cover;
      background: #000;
    }
    figcaption {
      padding: 8px 10px;
      font-size: 0.8rem;
      color: #ccc;
      word-break: break-all;
    }
    #lightbox {
      display: none;
      position: fixed;
      inset: 0;
      background: #000000ee;
      z-index: 100;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 24px;
    }
    #lightbox.open { display: flex; }
    #lightbox img {
      max-width: 95vw;
      max-height: 80vh;
      object-fit: contain;
      border-radius: 4px;
    }
    #lightbox .cap { margin-top: 12px; font-size: 1rem; color: #ddd; }
    #lightbox .nav {
      position: absolute;
      top: 50%;
      transform: translateY(-50%);
      font-size: 2rem;
      padding: 16px 20px;
      background: #ffffff22;
      border: none;
      color: #fff;
      cursor: pointer;
      border-radius: 8px;
    }
    #lightbox .prev { left: 12px; }
    #lightbox .next { right: 12px; }
    #lightbox .close {
      position: absolute;
      top: 12px;
      right: 16px;
      font-size: 1.5rem;
      background: none;
      border: none;
      color: #fff;
      cursor: pointer;
    }
    details {
      margin: 8px 16px 0;
      font-size: 0.8rem;
      color: #888;
    }
    pre { white-space: pre-wrap; margin: 8px 0 0; }
  </style>
</head>
<body>
  <header>
    <h1>${TITLE}</h1>
    <p class="hint">点击放大 · 方向键 ← → 切换 · Esc 关闭 · 再点一次标记候选（蓝框）</p>
  </header>
EOF

  if [[ -n "$README" ]]; then
    echo "<details><summary>README 时间轴</summary><pre>$README</pre></details>"
  fi

  echo '<div class="grid" id="grid">'
  for f in "${FILES[@]}"; do
    base="$(basename "$f")"
    echo "  <figure data-name=\"$base\"><img src=\"$base\" alt=\"$base\" loading=\"lazy\" /><figcaption>$base</figcaption></figure>"
  done
  echo '</div>'

  cat <<'EOF'
  <div id="lightbox" aria-hidden="true">
    <button class="close" type="button" aria-label="关闭">×</button>
    <button class="nav prev" type="button" aria-label="上一张">‹</button>
    <img id="lb-img" src="" alt="" />
    <button class="nav next" type="button" aria-label="下一张">›</button>
    <div class="cap" id="lb-cap"></div>
  </div>
  <script>
    const figures = [...document.querySelectorAll('#grid figure')];
    const names = figures.map(f => f.dataset.name);
    const lb = document.getElementById('lightbox');
    const lbImg = document.getElementById('lb-img');
    const lbCap = document.getElementById('lb-cap');
    let idx = 0;

    function show(i) {
      idx = (i + names.length) % names.length;
      lbImg.src = names[idx];
      lbCap.textContent = names[idx] + '  (' + (idx + 1) + '/' + names.length + ')';
      lb.classList.add('open');
    }

    figures.forEach((fig, i) => {
      fig.addEventListener('click', () => show(i));
      fig.addEventListener('dblclick', () => fig.classList.toggle('selected'));
    });

    document.querySelector('.close').onclick = () => lb.classList.remove('open');
    document.querySelector('.prev').onclick = () => show(idx - 1);
    document.querySelector('.next').onclick = () => show(idx + 1);
    lb.addEventListener('click', (e) => { if (e.target === lb) lb.classList.remove('open'); });

    document.addEventListener('keydown', (e) => {
      if (!lb.classList.contains('open')) return;
      if (e.key === 'Escape') lb.classList.remove('open');
      if (e.key === 'ArrowLeft') show(idx - 1);
      if (e.key === 'ArrowRight') show(idx + 1);
      if (e.key === 's' || e.key === 'S') figures[idx]?.classList.toggle('selected');
    });

    const selected = () => [...document.querySelectorAll('figure.selected')].map(f => f.dataset.name);
    console.log('双击标记候选；控制台输入 copy(selected()) 可复制已选文件名');
    window.selected = selected;
  </script>
</body>
</html>
EOF
} > "$OUT"

echo "wrote $OUT (${#FILES[@]} images)"
if [[ "$(uname)" == Darwin ]]; then
  echo "open: file://$OUT"
fi
