import { useSelector } from 'react-redux';

// Use throughout your app instead of plain `useSelector`
// This version uses inline typing to avoid RootState import issues
export const useAppSelector = <TSelected = unknown>(
  selector: (state: any) => TSelected
): TSelected => useSelector(selector);