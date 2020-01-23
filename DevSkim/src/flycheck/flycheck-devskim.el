;;; flycheck-devskim.el --- Flycheck: DevSkim support  -*- lexical-binding: t; -*-

;; Copyright (c) Microsoft Corporation

;; Author: Michael Scovetta <michael.scovetta@microsoft.com
;; Keywords: security, tools
;; Version: 0.1.0
;; URL: https://github.com/Microsoft/DevSkim
;; Package-Requires: ((emacs "25.3") (flycheck "31"))

;; All rights reserved.
;;
;; MIT License
;;
;; Permission is hereby granted, free of charge, to any person obtaining a copy
;; of this software and associated documentation files (the "Software"), to deal
;; in the Software without restriction, including without limitation the rights
;; to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
;; copies of the Software, and to permit persons to whom the Software is
;; furnished to do so, subject to the following conditions:
;;
;;   The above copyright notice and this permission notice shall be included in;
;; all copies or substantial portions of the Software.
;;
;;   THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
;; IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
;; FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
;; AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
;; LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
;; OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
;; SOFTWARE.

;;; Commentary:

;; DevSkim is a code analysis tool for multiple programming languages, based on
;; grep-style rules.
;;
; Usage:
;;
;; Load flycheck-devskim from wherever you placed it.
;; (load "~/.emacs.d/flycheck-devskim") 

;;; Code:

(require 'flycheck)

(defun flycheck-parse-devskim (output checker buffer)
  "Parse DevSkim warnings.
  CHECKER and BUFFER denote the CHECKER that returned OUTPUT and
  the BUFFER that was checked."
  (let ((errors nil))
    (dolist (message (car (flycheck-parse-json output)))
      (let-alist message
        (push
          (flycheck-error-new-at
           .start_line
           .start_column
           (pcase .severity
             (`"1" 'error)
             (`"2" 'error)
             (`"3" 'warning)
             (`"4" 'warning)
             (_    'info))
          (concat .rule_name " " .recommendation)
          :id .rule_id
          :checker checker
          :buffer buffer
          :filename .filename)
         errors)))
    (nreverse errors)))

(flycheck-define-checker devskim
  "A DevSkim checker for Flycheck.
  See URL `https://github.com/Microsoft/DevSkim'."
  :command ("devskim.exe"
   "analyze"
   "-f"
   "json"
   source)
  :error-parser flycheck-parse-devskim
  :modes (c-mode c++-mode python-mode)
  )

(add-to-list 'flycheck-checkers 'devskim)

(provide 'flycheck-devskim)

;;; flycheck-devskim.el ends here