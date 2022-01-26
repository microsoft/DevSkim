export interface JsonIssueRecord {
    filename: string;
    start_line: number;
    start_column: number;
    end_line: number;
    end_column: number;
    rule_name: string;
    rule_id: string;
    severity: Severity;
    description: string;
    match: string;
}

export enum Severity {
    None = 0,
    Critical = 1,
    Important = 2,
    Moderate = 4,
    BestPractice = 8,
    ManualReview = 16
}