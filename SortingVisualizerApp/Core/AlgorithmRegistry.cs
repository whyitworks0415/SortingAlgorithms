using SortingVisualizerApp.Algorithms;

namespace SortingVisualizerApp.Core;

public sealed class AlgorithmRegistry
{
    private readonly Dictionary<string, AlgorithmMetadata> _byId;

    public IReadOnlyList<AlgorithmMetadata> All { get; }

    public AlgorithmRegistry()
    {
        var items = new List<AlgorithmMetadata>();

        // ===== Basic / Comparison =====
        AddImplemented(items,
            category: "Basic Comparison",
            name: "Bubble",
            description: "Adjacent compare-swap baseline.\nReference implementation for event/sonification behavior.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: true,
            factory: static () => new BubbleSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Optimized Bubble Sort",
            description: "Bubble sort with last-swap boundary shrink.\nCuts unnecessary tail scans on near-sorted data.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: true,
            factory: static () => new OptimizedBubbleSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Cocktail Shaker Sort",
            description: "Bidirectional bubble passes in each round.\nMoves small and large outliers faster than one-way bubble.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: true,
            factory: static () => new CocktailShakerSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Comb Sort",
            description: "Gap-based compare-swap with shrinking factor.\nRemoves turtles early before adjacent finishing passes.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new CombSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Selection Sort",
            description: "Select minimum and place at front each pass.\nWrite-efficient classic in-place comparison sort.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new SelectionSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Double Selection Sort",
            description: "Select min and max in the same pass.\nPlaces both ends each iteration to reduce pass count.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new DoubleSelectionSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Bingo Sort",
            description: "Collects all current minimum duplicates at once.\nUseful for duplicate-heavy distributions.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new BingoSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Basic Comparison", "Bingo (Legacy)"),
            Name: "Bingo (Legacy)",
            Category: "Basic Comparison",
            Status: AlgorithmImplementationStatus.B,
            Description: "Legacy compatibility entry mapped to current Bingo Sort implementation.\nBehavior is identical; retained for historical naming continuity.",
            AverageComplexity: "O(n^2)",
            WorstComplexity: "O(n^2)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new BingoLegacyAlgorithm()));

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Cycle Sort",
            description: "Rotates elements to final positions in cycles.\nMinimizes write count among in-place sorts.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new CycleSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Insertion",
            description: "Shifting insertion sort.\nStable local insertion baseline.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: true,
            factory: static () => new InsertionSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Binary Insertion Sort",
            description: "Binary-search insertion point + shifting writes.\nReduces comparisons vs plain insertion sort.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: true,
            factory: static () => new BinaryInsertionSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Shell Sort",
            description: "Gap-based insertion passes with shrinking intervals.\nPractical speedup over pure insertion on medium N.",
            avg: "~O(n^1.3..n^2)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new ShellSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Gnome Sort",
            description: "Backward swap walk until local order is restored.\nSimple insertion-like swap-based behavior.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: true,
            factory: static () => new GnomeSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Odd-Even Sort",
            description: "Alternating odd/even compare-swap phases.\nEquivalent transposition-style phased sorting.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: true,
            factory: static () => new OddEvenSortAlgorithm());

        AddImplemented(items,
            category: "Basic Comparison",
            name: "Pancake Sort",
            description: "Uses prefix reversals to place maximums.\nEducational flip-based in-place sorting approach.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new PancakeSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Basic Comparison", "Stooge Sort"),
            Name: "Stooge Sort",
            Category: "Basic Comparison",
            Status: AlgorithmImplementationStatus.B,
            Description: "Legacy recursive stooge behavior with safe large-N fallback.\nPreserves educational event pattern without freezing at large sizes.",
            AverageComplexity: "O(n^2.709)",
            WorstComplexity: "O(n^2.709)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new StoogeSortLegacyAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Basic Comparison", "Slow Sort"),
            Name: "Slow Sort",
            Category: "Basic Comparison",
            Status: AlgorithmImplementationStatus.B,
            Description: "Legacy slowsort recursion with safe large-N fallback path.\nRetained as educational baseline, not performance-focused.",
            AverageComplexity: "O(n^(log2 3))",
            WorstComplexity: "Super-polynomial",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new SlowSortLegacyAlgorithm()));

        // ===== Divide & Conquer / Heaps / Hybrids =====
        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Merge",
            description: "Top-down merge sort.\nStable divide-and-conquer baseline.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: true,
            factory: static () => new MergeSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Bottom-Up Merge Sort",
            description: "Iterative merge by doubling run width.\nNon-recursive stable merge schedule.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: true,
            factory: static () => new BottomUpMergeSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Natural Merge Sort",
            description: "Detects natural ascending runs before merging.\nTakes advantage of existing order in data.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: true,
            factory: static () => new NaturalMergeSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Quick",
            description: "Lomuto partition quicksort.\nSimple in-place partition baseline.",
            avg: "O(n log n)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new QuickSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Three-Way Quick Sort",
            description: "Dutch-national-flag 3-way partition quicksort.\nImproves duplicate-key behavior.",
            avg: "O(n log n)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new ThreeWayQuickSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Dual-Pivot Quick Sort",
            description: "Partitions by two pivots into three regions.\nCommon practical quicksort variant.",
            avg: "O(n log n)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new DualPivotQuickSortExpandedAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "IntroSort",
            description: "Quicksort with depth-limit heap fallback.\nUses insertion sort for small partitions.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            factory: static () => new IntroSortExpandedAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Tournament Sort",
            description: "Tournament winner extraction over active contenders.\nEmits deterministic compare/write event stream.",
            avg: "O(n log n)",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new TournamentSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Weak Heap Sort",
            description: "Weak-heap merge structure with bit flags.\nHeap-family in-place sorting variant.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            factory: static () => new WeakHeapSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Heap",
            description: "Binary heap sort.\nStandard in-place heap baseline.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            factory: static () => new HeapSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "TimSort",
            description: "Simplified TimSort (min-run + merge).\nAdaptive hybrid baseline (stability not guaranteed in this reduced variant).",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            factory: static () => new TimSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "In-Place Merge Sort",
            description: "Gap-based in-place merge scheduling without external merge buffer.\nEmits merge/left/right range focus events via stage + range markers. Recommended N <= 200000.",
            avg: "O(n log^2 n)",
            worst: "O(n log^2 n)",
            stable: false,
            factory: static () => new InPlaceMergeSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Smooth Sort",
            description: "Leonardo-heap inspired smooth-sort stage model with in-place extraction.\nUses structure/boundary stage events for heap progression visibility. Recommended N <= 200000.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            factory: static () => new SmoothSortAlgorithm());

        AddImplemented(items,
            category: "Divide&Conquer",
            name: "Cartesian Tree Sort",
            description: "Builds a Cartesian tree over value/index keys and streams inorder output.\nStage markers separate key-order setup, tree build, and traversal output phases. Recommended N <= 150000.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: true,
            factory: static () => new CartesianTreeSortAlgorithm());

        // ===== Heap / Tree Core Structures =====
        AddImplemented(items,
            category: "Heap/Tree",
            name: "Binary Heap Sort",
            description: "Classic in-place binary heap construction and extraction.\nEmits heap-boundary and structure-highlight events per phase.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new BinaryHeapSortAlgorithm());

        AddImplemented(items,
            category: "Heap/Tree",
            name: "Binomial Heap Sort",
            description: "Binomial-heap style merge-degree simulation plus extraction ordering.\nEmits merge-tree and level-highlight events during consolidation.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new BinomialHeapSortAlgorithm());

        AddImplemented(items,
            category: "Heap/Tree",
            name: "Fibonacci Heap Sort",
            description: "Fibonacci-heap inspired root-list/consolidation simulation.\nEmits merge-tree and heap-boundary events while producing sorted output.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new FibonacciHeapSortAlgorithm());

        AddImplemented(items,
            category: "Heap/Tree",
            name: "AVL Tree Sort",
            description: "Self-balancing AVL insertion with explicit rotation events.\nOutputs sorted order through in-order traversal writes.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new AvlTreeSortAlgorithm());

        AddImplemented(items,
            category: "Heap/Tree",
            name: "Red-Black Tree Sort",
            description: "Left-leaning red-black insertion with rotation/color-flip stages.\nIn-order traversal emits sorted write stream.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new RedBlackTreeSortAlgorithm());

        AddImplemented(items,
            category: "Heap/Tree",
            name: "Treap Sort",
            description: "BST key ordering with heap-priority randomized balancing.\nRotation events show heap-order restoration during inserts.",
            avg: "O(n log n) average",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new TreapSortAlgorithm());

        AddImplemented(items,
            category: "Heap/Tree",
            name: "Splay Tree Sort",
            description: "Top-down splay insertion with zig/zig-zag rotations.\nIn-order traversal materializes sorted array state.",
            avg: "O(n log n) amortized",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new SplayTreeSortAlgorithm());

        AddImplemented(items,
            category: "Heap/Tree",
            name: "Skip List Sort",
            description: "Probabilistic multi-level linked ordering with randomized levels.\nTraversal on base level emits final sorted writes.",
            avg: "O(n log n) average",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new SkipListSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Heap/Tree", "B-Tree Sort"),
            Name: "B-Tree Sort",
            Category: "Heap/Tree",
            Status: AlgorithmImplementationStatus.B,
            Description: "Concept-stage B-Tree insertion/split simulation with key promotion markers.\nProduces sorted output while emitting split/merge/level events.",
            AverageComplexity: "O(n log n)",
            WorstComplexity: "O(n log n)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new BTreeSortAlgorithm()));

        // ===== Non-Comparison =====
        AddImplemented(items,
            category: "Non-Comparison",
            name: "Counting",
            description: "Counting sort for non-negative integer domain.\nStable counting-based placement.",
            avg: "O(n + k)",
            worst: "O(n + k)",
            stable: true,
            factory: static () => new CountingSortAlgorithm());

        AddImplemented(items,
            category: "Non-Comparison",
            name: "Pigeonhole Sort",
            description: "A-only substitution for Smooth Sort in this phase.\nTODO(phase-next): replace with true Smooth Sort A implementation.",
            avg: "O(n + range)",
            worst: "O(n + range)",
            stable: false,
            factory: static () => new PigeonholeSortAlgorithm());

        AddImplemented(items,
            category: "Non-Comparison",
            name: "Bucket Sort",
            description: "Range bucketing with per-bucket local sort and writeback.\nGood baseline for distribution-sensitive non-comparison sorting.",
            avg: "O(n + k) average",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new BucketSortAlgorithm());

        AddImplemented(items,
            category: "Non-Comparison",
            name: "Flash Sort",
            description: "Class-distribution flash phase followed by local class refinement.\nSimplified flash-style implementation focused on deterministic events.",
            avg: "O(n) average",
            worst: "O(n^2)",
            stable: false,
            factory: static () => new FlashSortAlgorithm());

        AddImplemented(items,
            category: "Non-Comparison",
            name: "Radix LSD",
            description: "Least-significant-digit radix sort.\nStable multi-pass digit sorting.",
            avg: "O(d * (n + b))",
            worst: "O(d * (n + b))",
            stable: true,
            factory: static () => new RadixLsdSortAlgorithm());

        AddImplemented(items,
            category: "Non-Comparison",
            name: "Radix MSD",
            description: "Most-significant-digit radix sort.\nRecursive high-digit-first partitioning.",
            avg: "O(d * (n + b))",
            worst: "O(d * (n + b))",
            stable: false,
            factory: static () => new RadixMsdSortAlgorithm());

        // ===== String =====
        AddStringImplemented(items,
            category: "String",
            name: "LSD String Sort",
            description: "Stable LSD radix pass from rightmost character.\nDesigned for String View bucket/pass visualization.",
            avg: "O(w * (n + r))",
            worst: "O(w * (n + r))",
            stable: true,
            supportedViews: SupportedViews.String,
            factory: static () => new LsdStringSortAlgorithm());

        AddStringImplemented(items,
            category: "String",
            name: "MSD String Sort",
            description: "Recursive MSD radix partition by character index.\nShows prefix-driven bucket refinement in String View.",
            avg: "O(w * (n + r))",
            worst: "O(w * (n + r))",
            stable: true,
            supportedViews: SupportedViews.String,
            factory: static () => new MsdStringSortAlgorithm());

        AddStringImplemented(items,
            category: "String",
            name: "Trie-based Sort",
            description: "Builds a trie over strings and emits DFS output order.\nString View shows trie growth stages via pass/char events.",
            avg: "O(totalChars + n log sigma)",
            worst: "O(totalChars + n log sigma)",
            stable: true,
            supportedViews: SupportedViews.String,
            factory: static () => new TrieBasedStringSortAlgorithm());

        AddStringImplemented(items,
            category: "String",
            name: "3-way String QuickSort",
            description: "MSD-style 3-way partition quicksort on character depth.\nHighlights equal-prefix partition ranges effectively.",
            avg: "O(n log n) average / prefix-sensitive",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.String,
            factory: static () => new ThreeWayStringQuickSortAlgorithm());

        AddStringImplemented(items,
            category: "String",
            name: "Suffix Array (Doubling)",
            description: "Constructs suffix-array style ordering using doubling passes.\nPass/rank events summarize SA stage progression in String View.",
            avg: "O(m log m)",
            worst: "O(m log^2 m)",
            stable: true,
            supportedViews: SupportedViews.String,
            factory: static () => new SuffixArrayConstructionAlgorithm());

        AddStringImplemented(items,
            category: "String",
            name: "Burst (String)",
            description: "Burst-trie style string sorting with trie growth and burst events.\nPrioritizes correctness/stability with explicit bucket movement visualization.",
            avg: "O(totalChars + n log sigma)",
            worst: "O(totalChars * log sigma)",
            stable: true,
            supportedViews: SupportedViews.String,
            factory: static () => new BurstStringSortAlgorithm());

        AddStringImplemented(items,
            category: "String",
            name: "Burst Sort",
            description: "Enhanced burst-trie string sort alias with identical stable behavior.\nKept as canonical naming variant for algorithm encyclopedia completeness.",
            avg: "O(totalChars + n log sigma)",
            worst: "O(totalChars * log sigma)",
            stable: true,
            supportedViews: SupportedViews.String,
            factory: static () => new BurstStringSortAlgorithm());

        // ===== Spatial =====
        AddSpatialImplemented(items,
            category: "Spatial",
            name: "Morton Order Sort",
            description: "Computes Morton keys and sorts points by key.\nPrimary algorithm for Spatial View.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Spatial,
            factory: static () => new MortonOrderSortAlgorithm());

        AddSpatialImplemented(items,
            category: "Spatial",
            name: "Z-Order Sort",
            description: "Computes Z-order keys and sorts points by key.\nKept as distinct industrial naming variant.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Spatial,
            factory: static () => new ZOrderSortAlgorithm());

        AddSpatialImplemented(items,
            category: "Spatial",
            name: "KD-Tree Order",
            description: "Builds KD-order by recursive median splits and traversal.\nEmits region highlight events for split planes.",
            avg: "O(n log^2 n)",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Spatial,
            factory: static () => new KdTreeOrderSortAlgorithm());

        AddSpatialImplemented(items,
            category: "Spatial",
            name: "Hilbert Curve Order",
            description: "Computes Hilbert curve keys for 2D points and sorts by key.\nImproves locality vs simple Morton in many spatial datasets.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Spatial,
            factory: static () => new HilbertOrderSortAlgorithm());

        AddSpatialImplemented(items,
            category: "Spatial",
            name: "QuadTree-based Ordering",
            description: "Recursively partitions points by quadtree regions and emits traversal order.\nRegionHighlight + KeyComputed events provide clear spatial phase context.",
            avg: "O(n log n) average",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Spatial,
            factory: static () => new QuadTreeOrderSortAlgorithm());

        AddSpatialImplemented(items,
            category: "Spatial",
            name: "Spatial Sort",
            description: "Baseline spatial ordering using lexicographic x-then-y key quantization.\nDeterministic 2D ordering policy used as the canonical Spatial Sort definition.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Spatial,
            factory: static () => new SpatialSortAlgorithm());

        // ===== Existing A kept =====
        AddImplemented(items,
            category: "Networks",
            name: "Bitonic",
            description: "Bitonic network schedule execution.\nSupports both Bars and dedicated Network timeline view.",
            avg: "O(n log^2 n)",
            worst: "O(n log^2 n)",
            stable: false,
            supportedViews: SupportedViews.Bars | SupportedViews.Network,
            factory: static () => new BitonicSortAlgorithm());

        AddImplemented(items,
            category: "Networks",
            name: "Odd-Even Merge Network",
            description: "Enhanced odd-even merge schedule with explicit stage boundaries.\nDesigned for dense stage/wire timeline playback.",
            avg: "O(n log^2 n)",
            worst: "O(n log^2 n)",
            stable: false,
            supportedViews: SupportedViews.Bars | SupportedViews.Network,
            factory: static () => new OddEvenMergeNetworkSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Networks", "Odd-Even Merge (Legacy)"),
            Name: "Odd-Even Merge (Legacy)",
            Category: "Networks",
            Status: AlgorithmImplementationStatus.B,
            Description: "Legacy naming wrapper over current odd-even merge network schedule.\nPreserves timeline/stage events while separating legacy catalog entry.",
            AverageComplexity: "O(n log^2 n)",
            WorstComplexity: "O(n log^2 n)",
            Stable: false,
            SupportedViews: SupportedViews.Network,
            Factory: static () => new OddEvenMergeLegacyAlgorithm()));

        AddImplemented(items,
            category: "Networks",
            name: "Bose-Nelson Sorting Network",
            description: "Small-N focused network schedule with staged compare-exchange execution.\nNetwork View emphasizes stage progression and active pairs.",
            avg: "O(n^2) (practical staged variant)",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Network,
            factory: static () => new BoseNelsonNetworkSortAlgorithm());

        AddImplemented(items,
            category: "Networks",
            name: "Pairwise Sorting Network",
            description: "Pairwise staged network with odd-even cleanup.\nProvides dense, conflict-free stage visualization for Network View.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Network,
            factory: static () => new PairwiseNetworkSortAlgorithm());

        AddImplemented(items,
            category: "External",
            name: "External Merge",
            description: "Run generation + in-memory k-way merge simulation.\nEmits run/merge timeline events for External view.",
            avg: "O(n log k)",
            worst: "O(n log k)",
            stable: true,
            supportedViews: SupportedViews.Bars | SupportedViews.External,
            factory: static () => new ExternalMergeSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("External", "Multiway Merge"),
            Name: "Multiway Merge",
            Category: "External",
            Status: AlgorithmImplementationStatus.A,
            Description: "K-way run merge with full output writeback.\nEmits run/read/write/merge-group events and produces sorted final output.",
            AverageComplexity: "O(n log k)",
            WorstComplexity: "O(n log k)",
            Stable: true,
            SupportedViews: SupportedViews.External,
            Factory: static () => new MultiwayMergeSortSimulationAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("External", "Replacement Selection"),
            Name: "Replacement Selection",
            Category: "External",
            Status: AlgorithmImplementationStatus.A,
            Description: "Heap-based replacement-selection run generation + final merge.\nEmits run lifecycle and writes fully sorted output.",
            AverageComplexity: "O(n log m)",
            WorstComplexity: "O(n log m)",
            Stable: false,
            SupportedViews: SupportedViews.External,
            Factory: static () => new ReplacementSelectionSimulationAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("External", "Polyphase Merge Sort"),
            Name: "Polyphase Merge Sort",
            Category: "External",
            Status: AlgorithmImplementationStatus.B,
            Description: "Polyphase-style multi-tape run redistribution and merge simulation in memory.\nEmits run-create/read/write and merge-group events with deterministic sorted output projection. Recommended N <= 120000.",
            AverageComplexity: "O(n log r)",
            WorstComplexity: "O(n log r)",
            Stable: true,
            SupportedViews: SupportedViews.External,
            Factory: static () => new PolyphaseMergeSortAlgorithm()));

        AddImplemented(items,
            category: "Special",
            name: "Topological (Kahn)",
            description: "Random DAG + Kahn queue processing.\nPrimary algorithm for dedicated Graph view.",
            avg: "O(V + E)",
            worst: "O(V + E)",
            stable: false,
            supportedViews: SupportedViews.Graph,
            factory: static () => new TopologicalSortAlgorithm());

        AddImplemented(items,
            category: "Special",
            name: "QuickSelect",
            description: "In-place selection to kth element with partition events.\nExecute path finalizes full ordering for Bars consistency.",
            avg: "O(n) average",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new QuickSelectAlgorithm());

        AddImplemented(items,
            category: "Special",
            name: "Partial Sort",
            description: "Top-k selection + local ordering, then full-order finalize for Bars validation.\nExposes reusable top-k helper for analysis tests.",
            avg: "O(n + k log k) average",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new PartialSortAlgorithm());

        AddImplemented(items,
            category: "Special",
            name: "Stable Partition",
            description: "Stable predicate partition baseline with deterministic write stream.\nFinal pass returns globally ordered state for Bars harness compatibility.",
            avg: "O(n)",
            worst: "O(n)",
            stable: true,
            supportedViews: SupportedViews.Bars,
            factory: static () => new StablePartitionAlgorithm());

        AddImplemented(items,
            category: "Special",
            name: "K-way Merge",
            description: "Split data into sorted runs then merge through a priority queue.\nIn-memory k-way merge baseline distinct from external simulation.",
            avg: "O(n log k)",
            worst: "O(n log k)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new KWayMergeAlgorithm());

        AddImplemented(items,
            category: "Special",
            name: "Counting Inversions",
            description: "Merge-based inversion counting with simultaneous ordering output.\nPublishes inversion count marker event after completion.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: true,
            supportedViews: SupportedViews.Bars,
            factory: static () => new CountingInversionsAlgorithm());

        AddImplemented(items,
            category: "Adaptive",
            name: "Adaptive Merge Sort",
            description: "Detects pre-existing runs, then merges adaptively.\nStable run-aware merge behavior for nearly sorted datasets.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: true,
            supportedViews: SupportedViews.Bars,
            factory: static () => new AdaptiveMergeSortAlgorithm());

        AddImplemented(items,
            category: "Adaptive",
            name: "Library Sort",
            description: "Binary-search insertion style adaptive insertion baseline.\nReduced comparisons on partially ordered inputs.",
            avg: "O(n log n) average",
            worst: "O(n^2)",
            stable: true,
            supportedViews: SupportedViews.Bars,
            factory: static () => new LibrarySortAlgorithm());

        AddImplemented(items,
            category: "Adaptive",
            name: "Patience Sort",
            description: "Pile construction + priority extraction adaptive sort.\nUseful for duplicate-heavy or partially ordered patterns.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new PatienceSortAlgorithm());

        AddImplemented(items,
            category: "Advanced",
            name: "PDQSort",
            description: "Simplified pattern-defeating quicksort with bad-partition budget.\nFalls back to heap strategy on persistent imbalance.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new PdqSortAlgorithm());

        AddImplemented(items,
            category: "Advanced",
            name: "SpreadSort",
            description: "Integer spreadsort hybrid (bucket spread + recursive refinement).\nEmits bucket/range events and sorts in logarithmic passes.",
            avg: "O(n log n) average",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new SpreadSortAlgorithm());

        AddImplemented(items,
            category: "Non-Comparison",
            name: "American Flag Sort",
            description: "In-place MSD radix partitioning by byte digits.\nUses bucket-mark phases and recursive sub-bucket refinement.",
            avg: "O(w * n)",
            worst: "O(w * n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new AmericanFlagSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Advanced", "GrailSort"),
            Name: "GrailSort",
            Category: "Advanced",
            Status: AlgorithmImplementationStatus.B,
            Description: "Concept-stage GrailSort visualization (block/run phases).\nEmits range/stage events and writes final sorted projection.",
            AverageComplexity: "O(n log n)",
            WorstComplexity: "O(n log n)",
            Stable: true,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new GrailSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Advanced", "IPS4o"),
            Name: "IPS4o",
            Category: "Advanced",
            Status: AlgorithmImplementationStatus.B,
            Description: "Concept-stage IPS4o simulation (sampling/partition buckets).\nEmits pivot/bucket phase events for Bars overlays.",
            AverageComplexity: "O(n log n)",
            WorstComplexity: "O(n log n)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new Ips4oConceptAlgorithm()));

        AddImplemented(items,
            category: "Advanced",
            name: "Block Sort",
            description: "Fixed-size block sort with local insertion and stable block merge passes.\nSimplified A implementation focused on correctness and clear block/merge range stages.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: true,
            supportedViews: SupportedViews.Bars,
            factory: static () => new BlockSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Advanced", "WikiSort"),
            Name: "WikiSort",
            Category: "Advanced",
            Status: AlgorithmImplementationStatus.B,
            Description: "Concept-stage WikiSort simulation: run discovery, block-buffer sizing, and staged block merges.\nFinal output is deterministically materialized through sorted writeback.",
            AverageComplexity: "O(n log n)",
            WorstComplexity: "O(n log n)",
            Stable: true,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new WikiSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Advanced", "FluxSort"),
            Name: "FluxSort",
            Category: "Advanced",
            Status: AlgorithmImplementationStatus.B,
            Description: "Concept-stage FluxSort simulation: sample/pivot probes, partition ranges, and merge/refinement stages.\nFinal output uses deterministic sorted writeback for Bars consistency.",
            AverageComplexity: "O(n log n)",
            WorstComplexity: "O(n log n)",
            Stable: true,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new FluxSortConceptAlgorithm()));

        AddImplemented(items,
            category: "Parallel",
            name: "Parallel QuickSort",
            description: "Task-based quicksort with depth-limited fork/join.\nEmits parallel task, queue depth, and partition quality events.",
            avg: "O(n log n)",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new ParallelQuickSortAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Parallel MergeSort",
            description: "Task-based merge sort with staged fork/join and merge events.\nProduces deterministic output with merge phase markers.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new ParallelMergeSortAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Parallel Multiway Merge",
            description: "Chunk-local parallel sort followed by k-way merge.\nEmits task queue events and merge phase boundaries.",
            avg: "O(n log k + local-sort)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new ParallelMultiwayMergeSortAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Parallel Merge",
            description: "Canonical alias for parallel merge family with deterministic parallel-stage events.\nA implementation focused on robust completion under validation constraints.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new ParallelMergeAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Parallel Quick",
            description: "Canonical alias for parallel quick family with partition-oriented stage events.\nA implementation optimized for deterministic test parity and throughput.",
            avg: "O(n log n)",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new ParallelQuickAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Parallel Heap Sort",
            description: "Parallel category heap baseline with queue-depth and heap-boundary events.\nProduces deterministic sorted output for large-N validation.",
            avg: "O(n log n)",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new ParallelHeapSortAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Sample Sort",
            description: "Parallel sample-based partition sort with bucketed local ordering.\nEmits bucket/range events and parallel scheduling markers.",
            avg: "O(n log n) average",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new SampleSortAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Hypercube Sort",
            description: "Hypercube-style dimension phase simulation with deterministic completion path.\nStage and queue-depth events highlight virtual exchange rounds.",
            avg: "O(n log n) average",
            worst: "O(n log n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new HypercubeSortAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "Odd-Even Transposition (Parallel)",
            description: "Parallel odd-even transposition passes with staged parity phases.\nA implementation tuned for clear event progression and correctness.",
            avg: "O(n^2)",
            worst: "O(n^2)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new ParallelOddEvenTranspositionSortAlgorithm());

        AddImplemented(items,
            category: "Parallel",
            name: "GPU Bitonic Sort",
            description: "Compute-shader bitonic sort path in app runtime.\nThis algorithm object acts as CPU-safe fallback and deterministic write stream.",
            avg: "O(n log^2 n)",
            worst: "O(n log^2 n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new GpuBitonicSortAlgorithm());

        AddImplemented(items,
            category: "Non-Comparison",
            name: "GPU Radix LSD Sort",
            description: "Compute-shader LSD radix sort path in app runtime.\nThis algorithm object acts as CPU-safe fallback and deterministic write stream.",
            avg: "O(d * n)",
            worst: "O(d * n)",
            stable: false,
            supportedViews: SupportedViews.Bars,
            factory: static () => new GpuRadixLsdSortAlgorithm());

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Industrial", "TeraSort"),
            Name: "TeraSort",
            Category: "Industrial",
            Status: AlgorithmImplementationStatus.B,
            Description: "Concept simulation of large-scale run generation and global merge phases.\nEmits run/stage events and deterministic sorted projection.",
            AverageComplexity: "O(n log n) distributed",
            WorstComplexity: "O(n log n) distributed",
            Stable: true,
            SupportedViews: SupportedViews.Bars | SupportedViews.External,
            Factory: static () => new TeraSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Industrial", "Hadoop Sort"),
            Name: "Hadoop Sort",
            Category: "Industrial",
            Status: AlgorithmImplementationStatus.B,
            Description: "Map-shuffle-reduce inspired sorting pipeline simulation.\nEmits stage/bucket/run events to visualize distributed partition flow.",
            AverageComplexity: "O(n log n) distributed",
            WorstComplexity: "O(n log n) distributed",
            Stable: true,
            SupportedViews: SupportedViews.Bars | SupportedViews.External,
            Factory: static () => new HadoopSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Industrial", "Distributed Sample Sort"),
            Name: "Distributed Sample Sort",
            Category: "Industrial",
            Status: AlgorithmImplementationStatus.B,
            Description: "Distributed sample-sort concept with sampled pivots and worker buckets.\nEmits stage/bucket/merge events and deterministic sorted output.",
            AverageComplexity: "O(n log n) average",
            WorstComplexity: "O(n^2)",
            Stable: false,
            SupportedViews: SupportedViews.Bars | SupportedViews.External,
            Factory: static () => new DistributedSampleSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Inefficient", "Bogo Sort"),
            Name: "Bogo Sort",
            Category: "Inefficient",
            Status: AlgorithmImplementationStatus.B,
            Description: "Bounded-attempt bogo simulation with fallback completion path.\nEmits stage and shuffle swap events without infinite looping.",
            AverageComplexity: "O((n+1)!)",
            WorstComplexity: "Unbounded (bounded in app)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new BogoSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Inefficient", "Bozo Sort"),
            Name: "Bozo Sort",
            Category: "Inefficient",
            Status: AlgorithmImplementationStatus.B,
            Description: "Random-swap bozo simulation with bounded retries and deterministic fallback.\nEmits stage/swap events for educational behavior.",
            AverageComplexity: "Very high (randomized)",
            WorstComplexity: "Unbounded (bounded in app)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new BozoSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Inefficient", "Miracle Sort"),
            Name: "Miracle Sort",
            Category: "Inefficient",
            Status: AlgorithmImplementationStatus.B,
            Description: "Conceptual miracle sort phase simulation.\nUses stage markers then deterministic writeback completion.",
            AverageComplexity: "Unknown / conceptual",
            WorstComplexity: "Unknown / conceptual",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new MiracleSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Inefficient", "Quantum Bogo"),
            Name: "Quantum Bogo",
            Category: "Inefficient",
            Status: AlgorithmImplementationStatus.B,
            Description: "Conceptual quantum-bogo measurement simulation with bounded trials.\nFalls back deterministically while keeping phase events visible.",
            AverageComplexity: "Conceptual / probabilistic",
            WorstComplexity: "Unbounded (bounded in app)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new QuantumBogoConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Inefficient", "Sleep Sort"),
            Name: "Sleep Sort",
            Category: "Inefficient",
            Status: AlgorithmImplementationStatus.B,
            Description: "Sleep-sort concept via virtual delay buckets instead of real thread sleeps.\nProduces deterministic output and level timing events.",
            AverageComplexity: "Timing-dependent / conceptual",
            WorstComplexity: "Timing-dependent / conceptual",
            Stable: true,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new SleepSortConceptAlgorithm()));

        items.Add(new AlgorithmMetadata(
            Id: MakeId("Inefficient", "Stalin Sort"),
            Name: "Stalin Sort",
            Category: "Inefficient",
            Status: AlgorithmImplementationStatus.B,
            Description: "Monotonic-keep phase simulation with rejected-element markers.\nCompletes deterministically via final writeback for multiset safety.",
            AverageComplexity: "O(n)",
            WorstComplexity: "O(n)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: static () => new StalinSortConceptAlgorithm()));

        All = items
            .OrderBy(static x => x.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _byId = All.ToDictionary(static x => x.Id, static x => x, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string id, out AlgorithmMetadata metadata)
    {
        return _byId.TryGetValue(id, out metadata!);
    }

    public bool TryCreate(string id, out AlgorithmMetadata metadata, out ISortAlgorithm? algorithm)
    {
        if (!_byId.TryGetValue(id, out metadata!))
        {
            algorithm = null;
            return false;
        }

        algorithm = metadata.Factory?.Invoke();
        return true;
    }

    public bool TryCreateString(string id, out AlgorithmMetadata metadata, out IStringSortAlgorithm? algorithm)
    {
        if (!_byId.TryGetValue(id, out metadata!))
        {
            algorithm = null;
            return false;
        }

        algorithm = metadata.StringFactory?.Invoke();
        return true;
    }

    public bool TryCreateSpatial(string id, out AlgorithmMetadata metadata, out ISpatialSortAlgorithm? algorithm)
    {
        if (!_byId.TryGetValue(id, out metadata!))
        {
            algorithm = null;
            return false;
        }

        algorithm = metadata.SpatialFactory?.Invoke();
        return true;
    }

    private static void AddSmoothDeferred(ICollection<AlgorithmMetadata> target)
    {
        target.Add(new AlgorithmMetadata(
            Id: MakeId("Divide&Conquer", "Smooth Sort"),
            Name: "Smooth Sort",
            Category: "Divide&Conquer",
            Status: AlgorithmImplementationStatus.B,
            Description: "Deferred in A-only fill: replaced by Pigeonhole Sort A for delivery risk control.\nTODO(phase-next): implement true Dijkstra smoothsort with Leonardo heaps.",
            AverageComplexity: "O(n log n)",
            WorstComplexity: "O(n log n)",
            Stable: false,
            SupportedViews: SupportedViews.Bars,
            Factory: null));
    }

    private static void AddImplemented(
        ICollection<AlgorithmMetadata> target,
        string category,
        string name,
        string description,
        string avg,
        string worst,
        bool stable,
        Func<ISortAlgorithm> factory,
        SupportedViews supportedViews = SupportedViews.Bars)
    {
        target.Add(new AlgorithmMetadata(
            Id: MakeId(category, name),
            Name: name,
            Category: category,
            Status: AlgorithmImplementationStatus.A,
            Description: description,
            AverageComplexity: avg,
            WorstComplexity: worst,
            Stable: stable,
            SupportedViews: supportedViews,
            Factory: factory));
    }

    private static void AddStringImplemented(
        ICollection<AlgorithmMetadata> target,
        string category,
        string name,
        string description,
        string avg,
        string worst,
        bool stable,
        SupportedViews supportedViews,
        Func<IStringSortAlgorithm> factory)
    {
        target.Add(new AlgorithmMetadata(
            Id: MakeId(category, name),
            Name: name,
            Category: category,
            Status: AlgorithmImplementationStatus.A,
            Description: description,
            AverageComplexity: avg,
            WorstComplexity: worst,
            Stable: stable,
            SupportedViews: supportedViews,
            Factory: null,
            StringFactory: factory,
            SpatialFactory: null));
    }

    private static void AddSpatialImplemented(
        ICollection<AlgorithmMetadata> target,
        string category,
        string name,
        string description,
        string avg,
        string worst,
        bool stable,
        SupportedViews supportedViews,
        Func<ISpatialSortAlgorithm> factory)
    {
        target.Add(new AlgorithmMetadata(
            Id: MakeId(category, name),
            Name: name,
            Category: category,
            Status: AlgorithmImplementationStatus.A,
            Description: description,
            AverageComplexity: avg,
            WorstComplexity: worst,
            Stable: stable,
            SupportedViews: supportedViews,
            Factory: null,
            StringFactory: null,
            SpatialFactory: factory));
    }

    private static void AddPlaceholders(ICollection<AlgorithmMetadata> target, string category, IEnumerable<string> names)
    {
        var supportedViews = CategoryDefaultSupportedViews(category);

        foreach (var name in names)
        {
            target.Add(new AlgorithmMetadata(
                Id: MakeId(category, name),
                Name: name,
                Category: category,
                Status: AlgorithmImplementationStatus.B,
                Description: "Concept registration for later phase",
                AverageComplexity: "-",
                WorstComplexity: "-",
                Stable: null,
                SupportedViews: supportedViews,
                Factory: null));
        }
    }

    private static SupportedViews CategoryDefaultSupportedViews(string category)
    {
        return category switch
        {
            "Networks" => SupportedViews.Network,
            "External" => SupportedViews.External,
            "Special" => SupportedViews.Bars | SupportedViews.Graph,
            "String" => SupportedViews.String,
            "Spatial" => SupportedViews.Spatial,
            _ => SupportedViews.Bars
        };
    }

    private static string MakeId(string category, string name)
    {
        static string Normalize(string value)
        {
            var chars = value.ToLowerInvariant()
                .Select(static c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray();
            return new string(chars).Replace("--", "-");
        }

        return $"{Normalize(category)}:{Normalize(name)}";
    }
}
