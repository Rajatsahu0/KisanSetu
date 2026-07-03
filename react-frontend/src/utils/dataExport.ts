import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import Papa from 'papaparse';
import { ChartData, ComparisonChartData, TrendLineData } from '@/types/chartData';

interface ExportData {
  title: string;
  chartData?: ChartData;
  comparisonData?: ComparisonChartData;
  trendData?: TrendLineData;
  insights?: string[];
  metadata?: {
    dateRange: string;
    dataType: string;
    generatedAt: string;
  };
}

/**
 * Export data to PDF format
 */
export const exportToPDF = async (data: ExportData): Promise<void> => {
  const doc = new jsPDF();
  let yPosition = 20;

  // Add title
  doc.setFontSize(18);
  doc.setFont('helvetica', 'bold');
  doc.text(data.title, 20, yPosition);
  yPosition += 15;

  // Add metadata
  if (data.metadata) {
    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');
    doc.text(`Date Range: ${data.metadata.dateRange}`, 20, yPosition);
    yPosition += 7;
    doc.text(`Data Type: ${data.metadata.dataType}`, 20, yPosition);
    yPosition += 7;
    doc.text(`Generated: ${data.metadata.generatedAt}`, 20, yPosition);
    yPosition += 15;
  }

  // Add chart data table
  if (data.chartData && data.chartData.series.length > 0) {
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text('Historical Data', 20, yPosition);
    yPosition += 10;

    const series = data.chartData.series[0];
    const tableData = series.points.map(point => [
      point.label,
      point.value.toFixed(2),
      new Date(point.timestamp).toLocaleDateString(),
    ]);

    autoTable(doc, {
      startY: yPosition,
      head: [['Label', 'Value', 'Date']],
      body: tableData,
      theme: 'grid',
      headStyles: { fillColor: [59, 130, 246] },
      margin: { left: 20, right: 20 },
    });

    yPosition = (doc as any).lastAutoTable.finalY + 15;
  }

  // Add comparison data
  if (data.comparisonData && data.comparisonData.periods.length > 0) {
    if (yPosition > 250) {
      doc.addPage();
      yPosition = 20;
    }

    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text('Period Comparison', 20, yPosition);
    yPosition += 10;

    const comparisonTableData = data.comparisonData.periods.map(period => [
      period.periodLabel,
      period.averageValue.toFixed(2),
      period.totalValue.toFixed(2),
      period.dataPointCount.toString(),
    ]);

    autoTable(doc, {
      startY: yPosition,
      head: [['Period', 'Average', 'Total', 'Data Points']],
      body: comparisonTableData,
      theme: 'grid',
      headStyles: { fillColor: [59, 130, 246] },
      margin: { left: 20, right: 20 },
    });

    yPosition = (doc as any).lastAutoTable.finalY + 15;
  }

  // Add trend data
  if (data.trendData) {
    if (yPosition > 250) {
      doc.addPage();
      yPosition = 20;
    }

    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text('Trend Analysis', 20, yPosition);
    yPosition += 10;

    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');
    doc.text(`Direction: ${data.trendData.direction}`, 20, yPosition);
    yPosition += 7;
    doc.text(`Slope: ${data.trendData.slope.toFixed(4)}`, 20, yPosition);
    yPosition += 7;
    doc.text(`Equation: ${data.trendData.equation}`, 20, yPosition);
    yPosition += 15;
  }

  // Add insights
  if (data.insights && data.insights.length > 0) {
    if (yPosition > 220) {
      doc.addPage();
      yPosition = 20;
    }

    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text('AI Insights', 20, yPosition);
    yPosition += 10;

    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');

    data.insights.forEach((insight, index) => {
      if (yPosition > 270) {
        doc.addPage();
        yPosition = 20;
      }

      const lines = doc.splitTextToSize(`${index + 1}. ${insight}`, 170);
      doc.text(lines, 20, yPosition);
      yPosition += lines.length * 7 + 5;
    });
  }

  // Save the PDF
  const fileName = `${data.title.replace(/\s+/g, '_')}_${Date.now()}.pdf`;
  doc.save(fileName);
};

/**
 * Export data to CSV format
 */
export const exportToCSV = (data: ExportData): void => {
  let csvData: any[] = [];

  // Add metadata as header rows
  if (data.metadata) {
    csvData.push(['Title', data.title]);
    csvData.push(['Date Range', data.metadata.dateRange]);
    csvData.push(['Data Type', data.metadata.dataType]);
    csvData.push(['Generated', data.metadata.generatedAt]);
    csvData.push([]); // Empty row
  }

  // Add chart data
  if (data.chartData && data.chartData.series.length > 0) {
    csvData.push(['Historical Data']);
    csvData.push(['Label', 'Value', 'Timestamp', 'Series']);

    data.chartData.series.forEach(series => {
      series.points.forEach(point => {
        csvData.push([
          point.label,
          point.value,
          point.timestamp,
          series.name,
        ]);
      });
    });

    csvData.push([]); // Empty row
  }

  // Add comparison data
  if (data.comparisonData && data.comparisonData.periods.length > 0) {
    csvData.push(['Period Comparison']);
    csvData.push(['Period', 'Average Value', 'Total Value', 'Data Points']);

    data.comparisonData.periods.forEach(period => {
      csvData.push([
        period.periodLabel,
        period.averageValue,
        period.totalValue,
        period.dataPointCount,
      ]);
    });

    csvData.push([]); // Empty row
  }

  // Add trend data
  if (data.trendData) {
    csvData.push(['Trend Analysis']);
    csvData.push(['Direction', data.trendData.direction]);
    csvData.push(['Slope', data.trendData.slope]);
    csvData.push(['Equation', data.trendData.equation]);
    csvData.push([]); // Empty row
  }

  // Add insights
  if (data.insights && data.insights.length > 0) {
    csvData.push(['AI Insights']);
    data.insights.forEach((insight, index) => {
      csvData.push([`${index + 1}`, insight]);
    });
  }

  // Convert to CSV and download
  const csv = Papa.unparse(csvData);
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  
  link.setAttribute('href', url);
  link.setAttribute('download', `${data.title.replace(/\s+/g, '_')}_${Date.now()}.csv`);
  link.style.visibility = 'hidden';
  
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
};

/**
 * Export chart as image (PNG)
 */
export const exportChartAsImage = (chartElement: HTMLCanvasElement, title: string): void => {
  const link = document.createElement('a');
  link.download = `${title.replace(/\s+/g, '_')}_${Date.now()}.png`;
  link.href = chartElement.toDataURL('image/png');
  link.click();
};
