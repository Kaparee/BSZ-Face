%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
% File:     urdpl.cls (LaTeX Document class "urdpl")
% Author:   Zbigniew Gomółka, Ewa Żesławska
% Version:  v2.1 <25.02.2026>
%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

\NeedsTeXFormat{LaTeX2e}
\ProvidesClass{urdpl}[2025/12/18 v2.0 UR diploma class]

\message{*** [urdpl.cls] ---  (ZG, EZ)   v2.1 <25.02.2026>  ***}

%---------------------------------------------------------------------------
% OPTIONS + BASE CLASS
%---------------------------------------------------------------------------

\newif\ifpdflatex
\pdflatexfalse

\DeclareOption{pdflatex}{\pdflatextrue}
\DeclareOption*{\PassOptionsToClass{\CurrentOption}{report}}

\ProcessOptions\relax
\LoadClass[a4paper,oneside]{report}
% (jeśli chcesz dwustronnie: a4paper,twoside,openright)

%---------------------------------------------------------------------------
% PACKAGES
%---------------------------------------------------------------------------

\RequirePackage[T1]{fontenc}
\RequirePackage{times}         % rozważ newtxtext/newtxmath jeśli chcesz nowocześniej
\RequirePackage{graphicx}
\RequirePackage{geometry}
\RequirePackage[dvipsnames]{xcolor}
\RequirePackage{fancyhdr}
\RequirePackage{url}
\RequirePackage{titlesec}
\RequirePackage{tocloft}
\RequirePackage[font=small, labelfont=bf, labelsep=period]{caption}
\RequirePackage{subcaption}
\RequirePackage{siunitx}
\RequirePackage{enumerate}
\RequirePackage{enumitem}
\RequirePackage[all,defaultlines=2]{nowidow}

%---------------------------------------------------------------------------
% GEOMETRY / COLORS / SPACING
%---------------------------------------------------------------------------

\geometry{lmargin=35mm, rmargin=25mm, tmargin=25mm, bmargin=25mm, foot=10mm, head=10mm}
\definecolor{Green}{rgb}{0.1,0.5,0.1}
\linespread{1.3}

%---------------------------------------------------------------------------
% INTERNAL MACROS (need @)
%---------------------------------------------------------------------------

\makeatletter

% --- default values to avoid "Undefined control sequence"
\providecommand\@shorttitlePL{}
\providecommand\@shorttitleEN{}
\providecommand\@shortauthor{}
\providecommand\@titlePL{}
\providecommand\@titleEN{}
\providecommand\@faculty{}
\providecommand\@thesistype{}
\providecommand\@supervisor{}
\providecommand\@acknowledgements{}
\providecommand\@department{}
\providecommand\@degreeprogramme{}
\providecommand\@noAlbum{}
\providecommand\@thesisDone{}

% --- setters (API for users)
\def\shorttitlePL#1     {\gdef\@shorttitlePL{#1}}
\def\shorttitleEN#1     {\gdef\@shorttitleEN{#1}}
\def\shortauthor#1      {\gdef\@shortauthor{#1}}
\def\titlePL#1          {\gdef\@titlePL{#1}}
\def\titleEN#1          {\gdef\@titleEN{#1}}
\def\faculty#1          {\gdef\@faculty{#1}}
\def\thesistype#1       {\gdef\@thesistype{#1}}
\def\supervisor#1       {\gdef\@supervisor{#1}}
\def\acknowledgements#1 {\gdef\@acknowledgements{#1}}
\def\department#1       {\gdef\@department{#1}}
\def\degreeprogramme#1  {\gdef\@degreeprogramme{#1}}
\def\noAlbum#1          {\gdef\@noAlbum{#1}}
\def\thesisDone#1       {\gdef\@thesisDone{#1}}

%---------------------------------------------------------------------------
% HEADERS / FOOTERS (fancyhdr)
%---------------------------------------------------------------------------

\pagestyle{fancy}
\fancyhf{} % czyści nagłówki i stopki

% nagłówek: tytuł sekcji po lewej, numer strony po prawej
\fancyhead[LO,RE]{\slshape\small \rightmark}
\fancyhead[RO,LE]{\bfseries \thepage}

% stopka: autor + krótki tytuł po lewej
\def\thesisheaders{%
  \fancyfoot[LO,LE]{\small \@shortauthor\quad\textit{\@shorttitlePL}}%
}
\thesisheaders

\renewcommand{\chaptermark}[1]{\markboth{\thechapter.\ #1}{}}
\renewcommand{\sectionmark}[1]{\markright{\thesection.\ #1}}

\renewcommand{\headrulewidth}{0.5pt}
\renewcommand{\footrulewidth}{0.5pt}

\frenchspacing
\sloppy
\flushbottom

%---------------------------------------------------------------------------
% TITLE PAGES
%---------------------------------------------------------------------------

\newcommand{\titlepages}{%
  %-------------------------
  % Strona tytułowa
  %-------------------------
  \clearpage
  \begin{titlepage}

% --- LOGO: góra, środek ---
\noindent\makebox[\textwidth][c]{%
  \includegraphics[scale=.30]{figures/LogoURWydzial}%
}

\begin{center}

  % --- Typ pracy ---
  {\sffamily\huge\bfseries\textsc{Biometryczne Systemy Zabezpieczeń}\par}

  % --- Tytuł pracy ---
  \vspace*{20mm}
  {\sffamily\LARGE\itshape \@titlePL\par}

  %
  \vfill
  %
  % --- Autor ---
  \sffamily{\Large \@author\par}
  \vspace*{3mm}
  % --- Numer albumu ---
  {\sffamily\large Nr albumu: \@noAlbum\par}
  \vspace*{3mm}
  % --- Kierunek ---
  \sffamily{\large Kierunek studiów: \@degreeprogramme\par }

  % --- Promotor ---
  \vspace*{7mm}
  {\sffamily \large \@thesisDone \@supervisor\par}

  \vfill

  % --- Miejsce i data ---
  {\sffamily Rzeszów, \@date\par}
\end{center}

\end{titlepage}

  %-------------------------
  % Podziękowania
  %-------------------------
  \clearpage
  \begin{titlepage}
  \thispagestyle{empty}
  \vspace*{15cm}\vfill
  \begin{flushright}
    \begin{minipage}{10cm}
      {\itshape \@acknowledgements}
    \end{minipage}
  \end{flushright}
  \end{titlepage}

  %-------------------------
  % Pusta strona (opcjonalnie)
  %-------------------------
  \clearpage
  \thispagestyle{empty}
  \null
  \clearpage

  % Jeśli chcesz wymusić start numeracji:
  \setcounter{page}{5}%
}

%---------------------------------------------------------------------------
% TITLES FORMAT
%---------------------------------------------------------------------------

\titleformat{\chapter}[block]
{\bfseries\LARGE}{\filright \LARGE\thechapter. }{0ex}{}
\titlespacing{\chapter}{0pt}{0mm}{8mm}

\titleformat{\section}[block]
{\bfseries\Large}{\filright \Large\thesection. }{0ex}{}
\titlespacing{\section}{7mm}{3mm plus 0mm minus 1mm}{3mm plus 0mm minus 1mm}

\titleformat{\subsection}[block]
{\bfseries\large}{\filright \large\thesubsection. }{0ex}{}
\titlespacing{\subsection}{7mm}{3mm}{2.5mm}

\titleformat{\subsubsection}[block]
{\bfseries\normalsize}{\filright \normalsize\thesubsubsection. }{0ex}{}
\titlespacing{\subsubsection}{7mm}{3mm plus 0mm minus 1mm}{3mm plus 0mm minus 1mm}

%---------------------------------------------------------------------------
% TOC SETTINGS
%---------------------------------------------------------------------------

\renewcommand{\cfttoctitlefont}{\bfseries\Large}
\renewcommand{\cftbeforetoctitleskip}{0mm}
\renewcommand{\cftaftertoctitleskip}{10mm}
\renewcommand{\cftchapleader}{\cftdotfill{\cftdot}}
\renewcommand{\cftsecleader}{\cftdotfill{\cftdot}}
\renewcommand{\cftsubsecleader}{\cftdotfill{\cftdot}}
\renewcommand{\cftchapaftersnum}{.}
\renewcommand{\cftsecaftersnum}{.}
\renewcommand{\cftsubsecaftersnum}{.}
\setlength{\cftparskip}{2pt}
\setlength{\cftbeforechapskip}{0mm}
\renewcommand{\cftchappagefont}{\normalsize}

%---------------------------------------------------------------------------
% SIUNITX SETTINGS
%---------------------------------------------------------------------------

\sisetup{
  output-decimal-marker = {,},
  group-separator = {~},
}

%---------------------------------------------------------------------------
% URL FONT
%---------------------------------------------------------------------------

\renewcommand\UrlFont{\rmfamily\itshape}

%---------------------------------------------------------------------------
% CAPTIONS
%---------------------------------------------------------------------------

\captionsetup[subfigure]{labelfont=md}
\captionsetup{
  subrefformat=parens,
  margin=2cm,
  singlelinecheck=on,
}

%---------------------------------------------------------------------------
% eqwhere ENV
%---------------------------------------------------------------------------

\SetLabelAlign{parright}{\parbox[t]{\labelwidth}{\raggedleft#1}}
\setlist[description]{style=multiline, topsep=10pt, align=parright}

\newlength{\descriptiondashgap}
\setlength{\descriptiondashgap}{0.5em}

\renewcommand*\descriptionlabel[1]{%
  \hspace \labelsep \normalfont \bfseries #1 \mdseries
  \hspace{\descriptiondashgap}--\hspace{\descriptiondashgap}%
}

\newlength{\eqwheremargin}
\setlength{\eqwheremargin}{2cm}

\newenvironment{eqwhere}[1][]
{%
  \setlength{\eqwheremargin}{#1}%
  \begin{description}[leftmargin=\eqwheremargin, itemsep=0cm, labelsep=0cm]
}
{%
  \end{description}
}

\makeatother
