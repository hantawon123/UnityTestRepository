#!/usr/bin/env python3
"""플레이 로그 격자 CSV를 히트맵 PNG 로 그립니다. 로컬에서 돌립니다.

    python Tools/analytics/heatmap.py 체류구역.csv -o dwell.png
    python Tools/analytics/heatmap.py 체류구역.csv --split-by 단계 -o dwell.png
    python Tools/analytics/heatmap.py 은신처.csv 체류구역.csv -o compare.png

입력은 Metabase 3·4번 화면(`backend/docs/analytics-dashboards.md`)의 CSV 내보내기입니다.
그 화면들이 이미 2m 격자로 묶어 주므로 이 스크립트는 원본 좌표가 아니라 격자와 가중치를
받습니다. SQL 은 여기서 건드리지 않습니다 - 화면과 그림이 같은 값을 보게 하려는 것입니다.

맵 그림은 아직 없습니다. --map 으로 나중에 깔 수 있게 뚫어 두었고, 없으면 좌표축만 나옵니다.
"""
import argparse
import csv
import sys

import numpy as np

# matplotlib 은 표준 라이브러리가 아닙니다. 없을 때 추적 대신 할 일을 알려 줍니다.
try:
    import matplotlib
    matplotlib.use("Agg")  # 창을 띄우지 않습니다. 파일로만 저장합니다.
    import matplotlib.pyplot as plt
    from matplotlib.colors import LogNorm, Normalize
except ImportError:
    sys.exit("matplotlib 이 없습니다. 로컬에서 'pip install matplotlib' 을 실행하세요.")


def use_korean_font():
    """제목과 축에 한글을 쓰므로 한글 폰트를 먼저 잡습니다.

    기본 폰트(DejaVu Sans)에는 한글 글리프가 없어서 '체류 샘플' 이 □□ □□ 로 나옵니다.
    경고가 줄줄이 찍히기는 하지만 그림은 그대로 저장되므로, 보기 전에는 깨진 것을
    모릅니다. 없으면 영어로만 쓰라고 알려 주고 계속합니다 - 그림 자체는 멀쩡합니다.
    """
    from matplotlib import font_manager

    installed = {f.name for f in font_manager.fontManager.ttflist}
    for name in ("Malgun Gothic", "NanumGothic", "NanumBarunGothic",
                 "AppleGothic", "Noto Sans KR", "Noto Sans CJK KR"):
        if name in installed:
            plt.rcParams["font.family"] = name
            # 한글 폰트로 바꾸면 마이너스 기호가 두부로 나옵니다. 좌표에 음수가 흔합니다.
            plt.rcParams["axes.unicode_minus"] = False
            return name
    print("한글 폰트를 찾지 못했습니다. 제목의 한글이 깨져 보일 수 있습니다.", file=sys.stderr)
    return None


# Metabase 는 화면에 보이는 이름 그대로 CSV 헤더를 씁니다. 그래서 한글이 옵니다.
# 별칭으로 알아보고, 못 알아보면 --x/--z/--w 로 직접 지정하게 합니다. 헤더를 맞추라고
# 요구하면 CSV 를 손으로 고치게 되고, 그 순간 화면과 그림이 갈라집니다.
X_ALIASES = ("x(2m)", "구역X", "gx", "x", "pos_x")
Z_ALIASES = ("z(2m)", "구역Z", "gz", "z", "pos_z")
W_ALIASES = ("체류 샘플", "숨긴 횟수", "발견된 횟수", "건수", "count", "rows", "n")
SPLIT_ALIASES = ("단계", "phase", "맵", "map_id")


def pick(header, aliases, given, what):
    """헤더에서 쓸 열을 고릅니다. 못 고르면 무엇을 줘야 하는지 보여 주고 멈춥니다."""
    if given:
        if given not in header:
            sys.exit(f"'{given}' 열이 CSV 에 없습니다. 있는 열: {', '.join(header)}")
        return given
    for alias in aliases:
        if alias in header:
            return alias
    sys.exit(
        f"{what} 열을 찾지 못했습니다. 있는 열: {', '.join(header)}\n"
        f"  --x / --z / --w 로 직접 지정하세요."
    )


def load(path, x_col, z_col, w_col, split_col):
    """CSV 한 장을 읽습니다. 반환은 (x, z, weight, split) 배열 넷."""
    # utf-8-sig 입니다. Metabase 내보내기에 BOM 이 붙어 오면 첫 헤더 이름에 BOM 이
    # 그대로 들어가 '맵' 이 '﻿맵' 이 되고, 별칭 검색이 조용히 실패합니다.
    with open(path, encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames is None:
            sys.exit(f"{path} 가 비어 있습니다.")
        header = reader.fieldnames
        xc = pick(header, X_ALIASES, x_col, "x")
        zc = pick(header, Z_ALIASES, z_col, "z")
        wc = pick(header, W_ALIASES, w_col, "가중치(건수)")
        sc = split_col if split_col else None
        if sc and sc not in header:
            sys.exit(f"'{sc}' 열이 CSV 에 없습니다. 있는 열: {', '.join(header)}")

        xs, zs, ws, ss = [], [], [], []
        for line, row in enumerate(reader, start=2):
            try:
                x, z, w = float(row[xc]), float(row[zc]), float(row[wc])
            except (TypeError, ValueError):
                # 빈 칸이 섞이는 것은 정상입니다(LEFT JOIN 이 만든 NULL). 세지 않고 넘깁니다.
                continue
            xs.append(x)
            zs.append(z)
            ws.append(w)
            ss.append(row[sc] if sc else "")

    if not xs:
        sys.exit(f"{path} 에서 읽을 수 있는 행이 없습니다.")
    return (np.array(xs), np.array(zs), np.array(ws), np.array(ss)), (xc, zc, wc)


def to_grid(x, z, w, cell, bounds):
    """흩어진 (x, z, w) 를 규칙적인 2차원 격자로 채웁니다.

    bounds 는 모든 패널이 함께 쓰는 (x0, x1, z0, z1) 입니다. 패널마다 자기 데이터로
    범위를 잡으면 크기도 눈금도 달라져, 화면상 같은 자리가 패널마다 다른 월드 좌표가
    됩니다. 나란히 놓고 비교하려고 만든 그림에서 그건 치명적입니다.

    값이 없는 칸은 0 이 아니라 NaN 입니다. 0 으로 두면 색이 칠해져 맵 전체가 덮이고,
    '아무도 안 간 곳'과 '조금 간 곳'이 구분되지 않습니다.
    """
    x0, x1, z0, z1 = bounds
    xs = np.arange(x0, x1 + cell, cell)
    zs = np.arange(z0, z1 + cell, cell)
    grid = np.full((len(zs), len(xs)), np.nan)

    xi = np.rint((x - x0) / cell).astype(int)
    zi = np.rint((z - z0) / cell).astype(int)
    for i, j, value in zip(zi, xi, w):
        # 같은 칸이 두 번 오면 더합니다. 단계로 나누지 않은 CSV 에서 일어납니다.
        grid[i, j] = value if np.isnan(grid[i, j]) else grid[i, j] + value

    # extent 는 칸의 바깥 모서리입니다. 칸 값을 중심 좌표로 주면 그림이 반 칸 밀립니다.
    extent = (x0, x1 + cell, z0, z1 + cell)
    return grid, extent


def draw(ax, grid, extent, norm, cmap, background, background_extent):
    if background is not None:
        ax.imshow(background, extent=background_extent, origin="upper", zorder=0)

    # origin='lower' 입니다. 유니티의 +z 는 위쪽이고 이미지의 행 0 은 위쪽이라,
    # 기본값(origin='upper')으로 두면 맵이 위아래로 뒤집힙니다. 벽이 없는 그림에서는
    # 뒤집혀도 그럴듯해 보여서 알아채기 어렵습니다.
    image = ax.imshow(
        grid,
        extent=extent,
        origin="lower",
        cmap=cmap,
        norm=norm,
        interpolation="nearest",  # 칸 하나가 한 칸으로 보이게. 보간하면 없는 값이 생깁니다.
        zorder=1,
        alpha=0.85 if background is not None else 1.0,
    )
    # 공간을 봅니다. 1:1 이 아니면 좁은 복도가 넓어 보이고 거리 감각이 무너집니다.
    ax.set_aspect("equal")
    ax.set_xlabel("x")
    ax.set_ylabel("z")
    return image


def main():
    parser = argparse.ArgumentParser(
        description="플레이 로그 격자 CSV 를 히트맵 PNG 로 그립니다.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("csv", nargs="+", help="Metabase 에서 내보낸 CSV. 둘까지 나란히 그립니다.")
    parser.add_argument("-o", "--out", default="heatmap.png", help="저장할 PNG 경로")
    parser.add_argument("--x", help="x 열 이름 (기본: 자동 인식)")
    parser.add_argument("--z", help="z 열 이름 (기본: 자동 인식)")
    parser.add_argument("--w", help="가중치 열 이름 (기본: 자동 인식)")
    parser.add_argument("--split-by", help="이 열의 값마다 패널을 나눕니다. 예: 단계")
    parser.add_argument("--cell", type=float, default=2.0, help="격자 한 칸의 크기(m). 기본 2")
    parser.add_argument("--log", action="store_true",
                        help="색을 로그 눈금으로. 한두 칸이 압도적으로 클 때 씁니다")
    parser.add_argument("--cmap", default="YlOrRd",
                        help="matplotlib 컬러맵. 기본은 흰 배경용(낮은 값이 옅음). "
                             "--map 으로 어두운 맵 위에 얹을 때는 inferno 가 낫습니다")
    parser.add_argument("--independent-scale", action="store_true",
                        help="패널마다 색 눈금을 따로 잡습니다. 기본은 공유(패널끼리 비교 가능)")
    parser.add_argument("--map", dest="map_image",
                        help="배경에 깔 맵 이미지. --extent 와 함께 씁니다")
    parser.add_argument("--extent", nargs=4, type=float, metavar=("X0", "Z0", "X1", "Z1"),
                        help="맵 이미지가 덮는 월드 좌표 범위")
    parser.add_argument("--title", help="그림 전체 제목")
    parser.add_argument("--dpi", type=int, default=140)
    args = parser.parse_args()

    use_korean_font()

    if len(args.csv) > 2:
        sys.exit("CSV 는 둘까지입니다. 셋 이상은 그림이 좁아 읽을 수 없습니다.")
    if args.map_image and not args.extent:
        sys.exit("--map 에는 --extent X0 Z0 X1 Z1 이 필요합니다. "
                 "맵 이미지가 어느 좌표를 덮는지 모르면 겹칠 수 없습니다.")

    background, background_extent = None, None
    if args.map_image:
        background = plt.imread(args.map_image)
        x0, z0, x1, z1 = args.extent
        background_extent = (x0, x1, z0, z1)

    # 먼저 전부 읽습니다. 공통 좌표 범위를 알아야 격자를 만들 수 있기 때문입니다.
    loaded = []
    for path in args.csv:
        (x, z, w, split), (_, _, wc) = load(path, args.x, args.z, args.w, args.split_by)
        label = path.rsplit("/", 1)[-1].rsplit("\\", 1)[-1]
        loaded.append((label, x, z, w, split, wc))

    bounds = (
        min(item[1].min() for item in loaded), max(item[1].max() for item in loaded),
        min(item[2].min() for item in loaded), max(item[2].max() for item in loaded),
    )

    # 패널 하나 = (제목, 격자, extent, 지표 이름).
    panels = []
    for label, x, z, w, split, wc in loaded:
        if args.split_by:
            for value in sorted(set(split)):
                keep = split == value
                grid, extent = to_grid(x[keep], z[keep], w[keep], args.cell, bounds)
                panels.append((f"{value} · {wc}", grid, extent, wc))
        else:
            grid, extent = to_grid(x, z, w, args.cell, bounds)
            panels.append((f"{label} · {wc}", grid, extent, wc))

    # 색 눈금은 **같은 지표끼리만** 공유합니다. 단계별 패널은 같은 값을 세므로 눈금을
    # 나누면 비교가 안 되고, 서로 다른 지표(숨긴 횟수 최대 9 / 체류 샘플 최대 948)를
    # 한 눈금에 놓으면 작은 쪽이 통째로 새까맣게 뭉갭니다. 열 이름으로 가릅니다.
    scales = {}
    for _, grid, _, metric in panels:
        finite = grid[~np.isnan(grid)]
        if finite.size == 0:
            continue
        lo, hi = scales.get(metric, (finite.min(), finite.max()))
        scales[metric] = (min(lo, finite.min()), max(hi, finite.max()))
    if not scales:
        sys.exit("모든 칸이 비어 있습니다. CSV 의 가중치 열을 확인하세요.")

    figure, axes = plt.subplots(
        1, len(panels), figsize=(6.4 * len(panels), 6.0), squeeze=False)
    for ax, (title, grid, extent, metric) in zip(axes[0], panels):
        finite = grid[~np.isnan(grid)]
        if args.independent_scale or metric not in scales:
            lo, hi = (finite.min(), finite.max()) if finite.size else (0, 1)
        else:
            lo, hi = scales[metric]
        if args.log:
            lo = max(lo, 1e-9)
        # lo == hi 이면(값이 한 종류) 정규화가 0 으로 나눕니다. 위를 조금 벌립니다.
        if hi <= lo:
            hi = lo + 1
        norm = LogNorm(vmin=lo, vmax=hi) if args.log else Normalize(vmin=lo, vmax=hi)

        cmap = plt.get_cmap(args.cmap).copy()
        # 값이 없는 칸. 배경 맵이 있으면 투명하게 비켜 주고, 없으면 연한 회색으로
        # 칠합니다. 흰색으로 두면 '값 없음'이 '값이 낮음'보다 밝아 보여서, 눈이
        # 순서를 거꾸로 읽습니다. 회색이면 회색<연한색<진한색으로 순서가 지켜집니다.
        cmap.set_bad(alpha=0.0) if background is not None else cmap.set_bad("#e6e6e6")
        image = draw(ax, grid, extent, norm, cmap, background, background_extent)
        ax.set_title(title)
        figure.colorbar(image, ax=ax, fraction=0.046, pad=0.04)

    if args.title:
        figure.suptitle(args.title)
    figure.tight_layout()
    figure.savefig(args.out, dpi=args.dpi, bbox_inches="tight")
    spread = []
    for _, grid, _, _ in panels:
        finite = grid[~np.isnan(grid)]
        if finite.size:
            spread.append((float(finite.min()), float(np.median(finite)), float(finite.max())))
    lo = min(s[0] for s in spread)
    hi = max(s[2] for s in spread)
    print(f"{args.out} 에 저장했습니다. 패널 {len(panels)}개, 값 {lo:g} ~ {hi:g}")
    worst = max(s[2] / s[1] for s in spread if s[1] > 0) if any(s[1] > 0 for s in spread) else 1
    if worst > 50 and not args.log:
        print(f"  한 칸이 중앙값의 {worst:.0f}배입니다. --log 를 붙이면 나머지가 보입니다.")


if __name__ == "__main__":
    main()
