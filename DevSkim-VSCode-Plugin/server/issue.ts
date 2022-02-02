export interface JsonIssueRecord {
    filename: string;
    start_line: number;
    start_column: number;
    end_line: number;
    end_column: number;
    rule_id: string;
    rule_name: string;
    severity: Severity;
    description: string;
    fixes: Fix[];
}

export enum Severity {
    None = 0,
    Critical = 1,
    Important = 2,
    Moderate = 4,
    BestPractice = 8,
    ManualReview = 16
}

export interface Fix{
    name: string;
    range: DevSkimRange;
    replacement: string;
}

export interface DevSkimRange{
    start_line: number;
    start_column: number;
    end_line: number;
    end_column: number;
}