import { lazy, Suspense } from 'react';
import { SkeletonChart } from '@/components/common';

// Lazy load the chart container to reduce initial bundle size
const ChartContainer = lazy(() => import('./ChartContainer').then(m => ({ default: m.ChartContainer })));

export const LazyChartContainer = (props: any) => {
  return (
    <Suspense fallback={<SkeletonChart />}>
      <ChartContainer {...props} />
    </Suspense>
  );
};
